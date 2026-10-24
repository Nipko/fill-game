// Stack global en us-east-1 (Q-AWS-5): AWS Budgets (Q-AWS-6) y, solo con dominio, los certificados
// de ACM para CloudFront (que exige us-east-1). Con DNS externo (D-044) se validan por CNAME fuera de AWS.
import { Annotations, CfnOutput, Fn, Stack } from 'aws-cdk-lib';
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
  /**
   * Certificado de la web, solo con dominio. Con Route 53, uno compartido con la CDN (`<dominio>`, `www.` y `cdn.`);
   * con DNS externo, solo `<dominio>` y `www.<dominio>`.
   */
  readonly cloudFrontCertificate?: Certificate;
  /** Certificado de `cdn.<dominio>`: el compartido con Route 53; con DNS externo, uno propio y solo con `domainExtras=cdn`. */
  readonly cdnCertificate?: Certificate;

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

    // ---- Certificados de CloudFront (solo con config.domain; Q-AWS-9 y D-044) ----
    const domain = config.domain;
    if (domain?.hostedZoneId !== undefined) {
      // Route 53: un certificado para la web y la CDN, validado con registros en la zona.
      const zone = HostedZone.fromHostedZoneAttributes(this, 'Zone', {
        hostedZoneId: domain.hostedZoneId,
        zoneName: domain.name,
      });
      this.cloudFrontCertificate = new Certificate(this, 'CloudFrontCertificate', {
        domainName: domain.name,
        subjectAlternativeNames: [`www.${domain.name}`, `cdn.${domain.name}`],
        validation: CertificateValidation.fromDns(zone),
      });
      this.cdnCertificate = this.cloudFrontCertificate;
    } else if (domain) {
      // DNS externo (Cloudflare): validación por DNS sin zona, así que CDK no crea ningún registro. CloudFormation
      // espera con el certificado en «pendiente de validación» hasta que el dueño crea los CNAME (README, «Conectar
      // tintalu.com»). La CDN lleva su propio certificado para que añadirla más tarde no reemplace el de la web.
      this.cloudFrontCertificate = new Certificate(this, 'WebCertificate', {
        domainName: domain.name,
        subjectAlternativeNames: [`www.${domain.name}`],
        validation: CertificateValidation.fromDns(),
      });
      externalDnsOutputs(this, 'Web', this.cloudFrontCertificate, [domain.name, `www.${domain.name}`]);
      if (domain.cdn) {
        this.cdnCertificate = new Certificate(this, 'CdnCertificate', {
          domainName: `cdn.${domain.name}`,
          validation: CertificateValidation.fromDns(),
        });
        externalDnsOutputs(this, 'Cdn', this.cdnCertificate, [`cdn.${domain.name}`]);
      }
    }
  }
}

/**
 * Salidas de un certificado validado fuera de AWS. Los CNAME de validación no son un atributo de CloudFormation, así que
 * la salida dice dónde leerlos: la consola de ACM o `aws acm describe-certificate` con el ARN. Las salidas aparecen al
 * terminar el despliegue (con el certificado ya emitido); mientras espera, los mismos CNAME salen en la consola de ACM
 * y en los eventos del stack («Content of DNS Record is: …»).
 */
export function externalDnsOutputs(stack: Stack, prefix: string, certificate: Certificate, names: readonly string[]): void {
  new CfnOutput(stack, `${prefix}CertificateArn`, {
    description: `Certificado de ACM de ${names.join(' y ')} (validación por DNS externo).`,
    value: certificate.certificateArn,
  });
  new CfnOutput(stack, `${prefix}CertificateValidation`, {
    description: `CNAME de validación de ${names.join(' y ')}: créalos en el DNS externo, sin proxy, y no los borres (ACM los usa para renovar).`,
    value: Fn.join('', [
      `Consola de ACM (${stack.region}) › Certificados › ${names[0]} › Dominios, o: aws acm describe-certificate --region ${stack.region} --certificate-arn `,
      certificate.certificateArn,
      ' --query "Certificate.DomainValidationOptions[].ResourceRecord"',
    ]),
  });
}
