// Stack global en us-east-1 (Q-AWS-5): AWS Budgets (Q-AWS-6) y, solo con dominio, el certificado
// de ACM para CloudFront (que exige us-east-1).
import { Annotations, Stack } from 'aws-cdk-lib';
import type { StackProps } from 'aws-cdk-lib';
import { CfnBudget } from 'aws-cdk-lib/aws-budgets';
import { Certificate, CertificateValidation } from 'aws-cdk-lib/aws-certificatemanager';
import { HostedZone } from 'aws-cdk-lib/aws-route53';
import type { Construct } from 'constructs';
import type { AfConfig } from './config.ts';

export interface GlobalStackProps extends StackProps {
  readonly config: AfConfig;
}

export class GlobalStack extends Stack {
  /** Certificado de la web y de la CDN (`<dominio>`, `www.<dominio>` y `cdn.<dominio>`), solo con dominio. */
  readonly cloudFrontCertificate?: Certificate;

  constructor(scope: Construct, id: string, props: GlobalStackProps) {
    super(scope, id, props);
    const { config } = props;

    // ---- Presupuesto mensual con avisos al 50, 80 y 100 % (Q-AWS-6) ----
    const subscribers = config.alertEmail
      ? [{ subscriptionType: 'EMAIL', address: config.alertEmail }]
      : undefined;
    if (!subscribers) {
      Annotations.of(this).addWarningV2(
        'af:budget-no-email',
        'Sin -c alertEmail=<correo> el presupuesto se crea sin avisos por correo.',
      );
    }
    new CfnBudget(this, 'MonthlyBudget', {
      budget: {
        budgetName: `${config.prefix}-monthly`,
        budgetType: 'COST',
        timeUnit: 'MONTHLY',
        budgetLimit: { amount: config.budgetUsd, unit: 'USD' },
      },
      notificationsWithSubscribers: subscribers
        ? config.budgetThresholdsPct.map((pct) => ({
            notification: {
              notificationType: 'ACTUAL',
              comparisonOperator: 'GREATER_THAN',
              threshold: pct,
              thresholdType: 'PERCENTAGE',
            },
            subscribers,
          }))
        : undefined,
    });

    // ---- Certificado de CloudFront (solo con config.domain; Q-AWS-9) ----
    if (config.domain) {
      const zone = HostedZone.fromHostedZoneAttributes(this, 'Zone', {
        hostedZoneId: config.domain.hostedZoneId,
        zoneName: config.domain.name,
      });
      this.cloudFrontCertificate = new Certificate(this, 'CloudFrontCertificate', {
        domainName: config.domain.name,
        subjectAlternativeNames: [`www.${config.domain.name}`, `cdn.${config.domain.name}`],
        validation: CertificateValidation.fromDns(zone),
      });
    }
  }
}
