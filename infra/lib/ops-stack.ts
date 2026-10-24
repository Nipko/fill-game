// F1-5 · ops-stack: alarmas del plan §2 con aviso por SNS al correo del dueño (por contexto).
// La retención de 30 días de los logs la fijan los stacks que crean cada log group; el
// presupuesto (Q-AWS-6) vive en el GlobalStack, en us-east-1.
import { Annotations, Duration, Stack } from 'aws-cdk-lib';
import type { StackProps } from 'aws-cdk-lib';
import {
  Alarm,
  ComparisonOperator,
  MathExpression,
  Metric,
  TreatMissingData,
} from 'aws-cdk-lib/aws-cloudwatch';
import type { IMetric } from 'aws-cdk-lib/aws-cloudwatch';
import { SnsAction } from 'aws-cdk-lib/aws-cloudwatch-actions';
import { Topic } from 'aws-cdk-lib/aws-sns';
import { EmailSubscription } from 'aws-cdk-lib/aws-sns-subscriptions';
import type { Construct } from 'constructs';
import type { ApiStack } from './api-stack.ts';
import type { AfConfig } from './config.ts';
import type { TableV2 } from 'aws-cdk-lib/aws-dynamodb';
import type { AfTables } from './data-stack.ts';
import { ack } from './nag.ts';

export interface OpsStackProps extends StackProps {
  readonly config: AfConfig;
  readonly api: ApiStack;
  readonly tables: AfTables;
}

/** Espacio de nombres de las métricas propias (las Lambdas las emiten en formato EMF, sin permisos extra). */
export const METRICS_NAMESPACE = 'AF/Backend';

export class OpsStack extends Stack {
  readonly topic: Topic;
  readonly alarms: Alarm[] = [];

  constructor(scope: Construct, id: string, props: OpsStackProps) {
    super(scope, id, props);
    const { config, api, tables } = props;

    this.topic = new Topic(this, 'AlarmTopic', {
      topicName: `${config.prefix}-alarms`,
      displayName: `${config.prefix} alarmas`,
      enforceSSL: true,
    });
    ack(this.topic, 'AwsSolutions-SNS2', 'CloudWatch no puede publicar en un tema cifrado con la clave gestionada aws/sns y una CMK cuesta más que el riesgo: los avisos no llevan datos personales, solo el nombre de la alarma.');
    if (config.alertEmail) {
      this.topic.addSubscription(new EmailSubscription(config.alertEmail));
    } else {
      Annotations.of(this).addWarningV2('af:alarms-no-email', 'Sin -c alertEmail=<correo> las alarmas no avisan a nadie.');
    }
    const action = new SnsAction(this.topic);

    const alarm = (
      logicalId: string,
      description: string,
      metric: IMetric,
      threshold: number,
      evaluationPeriods = 1,
    ): void => {
      const a = new Alarm(this, logicalId, {
        alarmName: `${config.prefix}-${logicalId.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase()}`,
        alarmDescription: description,
        metric,
        threshold,
        evaluationPeriods,
        comparisonOperator: ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        treatMissingData: TreatMissingData.NOT_BREACHING,
      });
      a.addAlarmAction(action);
      this.alarms.push(a);
    };
    const fiveMin = Duration.minutes(5);

    // ---- Lambda: errores y throttles de cada función ----
    for (const [name, f] of Object.entries(api.functions)) {
      const pascal = name.replace(/(^|-)(\w)/g, (_m, _s: string, c: string) => c.toUpperCase());
      alarm(`${pascal}Errors`, `Errores de ${name} (plan §2).`, f.fn.metricErrors({ period: fiveMin, statistic: 'Sum' }), 1);
      alarm(`${pascal}Throttles`, `Throttles de ${name} (plan §2).`, f.fn.metricThrottles({ period: fiveMin, statistic: 'Sum' }), 1);
    }

    // ---- API: 5xx ----
    alarm('Api5xx', '5xx de la HTTP API (plan §2).', api.api.metricServerError({ period: fiveMin, statistic: 'Sum' }), 5);

    // ---- Job diario: mensajes en la DLQ = ejecución fallida tras los reintentos ----
    alarm(
      'VoidedJobFailed',
      'voidedPurchasesJob agotó los reintentos y su evento está en la DLQ (plan §2).',
      api.jobDlq.metricApproximateNumberOfMessagesVisible({ period: fiveMin, statistic: 'Maximum' }),
      1,
    );

    // ---- DynamoDB: throttles de lectura y escritura por tabla ----
    for (const [key, table] of Object.entries(tables) as Array<[string, TableV2]>) {
      const pascal = key.charAt(0).toUpperCase() + key.slice(1);
      const m = (metricName: string): Metric =>
        new Metric({
          namespace: 'AWS/DynamoDB',
          metricName,
          dimensionsMap: { TableName: table.tableName },
          period: fiveMin,
          statistic: 'Sum',
        });
      alarm(
        `${pascal}${key === 'leagueBoard' ? 'Table' : ''}Throttles`,
        `Throttles de DynamoDB en ${key} (plan §2).`,
        new MathExpression({
          expression: 'FILL(r, 0) + FILL(w, 0)',
          usingMetrics: { r: m('ReadThrottleEvents'), w: m('WriteThrottleEvents') },
          period: fiveMin,
          label: `${key} throttles`,
        }),
        1,
      );
    }

    // ---- Cuota de integridad: 70 % de 10.000 al día (plan §1; F5-4). La emite la Lambda (EMF). ----
    alarm(
      'IntegrityQuota',
      'Comprobaciones de integridad del día por encima del 70 % de la cuota gratuita (10.000/día).',
      new Metric({
        namespace: METRICS_NAMESPACE,
        metricName: 'IntegrityChecks',
        dimensionsMap: { Env: config.envName },
        period: Duration.days(1),
        statistic: 'Sum',
      }),
      config.integrityDailyAlarm,
    );
  }
}
