// F1-2 · web-stack: web estática en S3 privado + CloudFront con OAC, TLS 1.2+, cabeceras de seguridad
// y noindex salvo en prod con dominio. Dominio y ACM solo con config.domain (Q-AWS-9; con DNS externo, D-044).
// Las rutas del borrado por web (/v1/account/delete-*) pasan por esta misma distribución hacia la API: mismo origen
// y sin CORS. Sin API (modo solo web, D-044) no hay esa ruta y config.json desactiva el formulario.
import { CfnOutput, Duration, Fn, RemovalPolicy, Stack } from 'aws-cdk-lib';
import type { StackProps } from 'aws-cdk-lib';
import type { ICertificate } from 'aws-cdk-lib/aws-certificatemanager';
import type { BehaviorOptions } from 'aws-cdk-lib/aws-cloudfront';
import {
  AllowedMethods,
  CachePolicy,
  Distribution,
  Function as CfFunction,
  FunctionCode,
  FunctionEventType,
  FunctionRuntime,
  HeadersFrameOption,
  HeadersReferrerPolicy,
  HttpVersion,
  OriginProtocolPolicy,
  OriginRequestCookieBehavior,
  OriginRequestHeaderBehavior,
  OriginRequestPolicy,
  OriginRequestQueryStringBehavior,
  OriginSslPolicy,
  PriceClass,
  ResponseHeadersPolicy,
  SecurityPolicyProtocol,
  ViewerProtocolPolicy,
} from 'aws-cdk-lib/aws-cloudfront';
import { HttpOrigin, S3BucketOrigin } from 'aws-cdk-lib/aws-cloudfront-origins';
import { LogGroup, RetentionDays } from 'aws-cdk-lib/aws-logs';
import { ARecord, AaaaRecord, HostedZone, RecordTarget } from 'aws-cdk-lib/aws-route53';
import { CloudFrontTarget } from 'aws-cdk-lib/aws-route53-targets';
import type { Bucket, CfnBucket } from 'aws-cdk-lib/aws-s3';
import { BucketDeployment, CacheControl, Source } from 'aws-cdk-lib/aws-s3-deployment';
import type { Construct } from 'constructs';
import type { AfConfig } from './config.ts';
import { AccessLogsBucket, privateBucket } from './constructs/private-bucket.ts';
import { ack } from './nag.ts';
import { deployableSiteDir, isIndexable } from './web-site.ts';

export interface WebStackProps extends StackProps {
  readonly config: AfConfig;
  /** Id de la HTTP API, para enrutar /v1/account/delete-* desde la web. Sin él (solo web), el borrado web está desactivado. */
  readonly apiId?: string;
  /** Certificado de us-east-1 (GlobalStack), solo con dominio. */
  readonly certificate?: ICertificate;
}

/** CSP de la web estática: nada de terceros; las llamadas a la API van al mismo origen. */
export const WEB_CSP = [
  "default-src 'self'",
  "script-src 'self'",
  "style-src 'self'",
  "img-src 'self' data:",
  "font-src 'self'",
  "connect-src 'self'",
  "object-src 'none'",
  "base-uri 'self'",
  "form-action 'self'",
  "frame-ancestors 'none'",
  'upgrade-insecure-requests',
].join('; ');

/** Archivo de configuración de despliegue que lee la web (no está en web/site/: lo escribe este stack). */
export const WEB_RUNTIME_CONFIG_FILE = 'config.json';

/** Configuración de despliegue que lee el script del borrado (`/config.json`). */
export type WebRuntimeConfig =
  | { readonly deleteEnabled: true; readonly apiBase: string }
  | { readonly deleteEnabled: false };

/**
 * Configuración de despliegue de la web. Con API, el borrado por web está activo y la base es vacía = mismo origen,
 * porque esta distribución reenvía /v1/account/delete-* a la API (y la CSP solo permite connect-src 'self'). Sin API
 * (modo solo web, D-044), desactivado: la página dice que estará disponible pronto y no envía nada. Al desplegar la
 * web con la API (sin `webOnly`), se activa sola.
 */
export function webRuntimeConfig(_config: AfConfig, hasApi: boolean): WebRuntimeConfig {
  return hasApi ? { deleteEnabled: true, apiBase: '' } : { deleteEnabled: false };
}

/** Reescribe /ruta/ y /ruta a /ruta/index.html (las páginas de F1-3 van por carpeta de idioma). */
const INDEX_REWRITE = `function handler(event) {
  var request = event.request;
  var uri = request.uri;
  if (uri.endsWith('/')) {
    request.uri = uri + 'index.html';
  } else if (uri.lastIndexOf('.') < uri.lastIndexOf('/')) {
    request.uri = uri + '/index.html';
  }
  return request;
}`;

export class WebStack extends Stack {
  readonly distribution: Distribution;

  constructor(scope: Construct, id: string, props: WebStackProps) {
    super(scope, id, props);
    const { config } = props;

    const logs = new AccessLogsBucket(this, 'AccessLogs', config).bucket;
    const site = privateBucket(this, 'SiteBucket', {
      config,
      logs,
      logPrefix: 's3/web/',
      bucketName: `${config.prefix}-web-${this.account}`,
      versioned: true,
      lifecycleRules: [{ id: 'old-versions', noncurrentVersionExpiration: Duration.days(30) }],
    });

    const headers = new ResponseHeadersPolicy(this, 'SecurityHeaders', {
      responseHeadersPolicyName: `${config.prefix}-web-headers`,
      comment: 'Cabeceras de seguridad de la web (F1-2).',
      securityHeadersBehavior: {
        contentSecurityPolicy: { contentSecurityPolicy: WEB_CSP, override: true },
        contentTypeOptions: { override: true },
        frameOptions: { frameOption: HeadersFrameOption.DENY, override: true },
        referrerPolicy: { referrerPolicy: HeadersReferrerPolicy.STRICT_ORIGIN_WHEN_CROSS_ORIGIN, override: true },
        strictTransportSecurity: {
          accessControlMaxAge: Duration.days(730),
          includeSubdomains: true,
          preload: false,
          override: true,
        },
        xssProtection: { protection: false, override: true },
      },
      customHeadersBehavior: {
        customHeaders: [
          { header: 'Permissions-Policy', value: 'camera=(), microphone=(), geolocation=(), payment=(), usb=()', override: true },
          { header: 'Cross-Origin-Opener-Policy', value: 'same-origin', override: true },
          // Solo se indexa prod con dominio (D-044); dev y prod en *.cloudfront.net, nunca (F1-2).
          ...(isIndexable(config) ? [] : [{ header: 'X-Robots-Tag', value: 'noindex, nofollow', override: true }]),
        ],
      },
    });

    const indexRewrite = new CfFunction(this, 'IndexRewrite', {
      functionName: `${config.prefix}-web-index-rewrite`,
      comment: 'Sirve index.html en las rutas de carpeta.',
      runtime: FunctionRuntime.JS_2_0,
      code: FunctionCode.fromInline(INDEX_REWRITE),
    });

    // Ruta del borrado por web hacia la API, solo si hay API (sin ella, modo solo web: D-044).
    const apiBehaviors: Record<string, BehaviorOptions> = {};
    if (props.apiId !== undefined) {
      const apiOrigin = new HttpOrigin(`${props.apiId}.execute-api.${config.region}.${this.urlSuffix}`, {
        protocolPolicy: OriginProtocolPolicy.HTTPS_ONLY,
        originSslProtocols: [OriginSslPolicy.TLS_V1_2],
      });

      // Solo lo que necesita el borrado por web: el cuerpo JSON. Sin Host, cookies, query ni
      // CloudFront-Viewer-Address: la API también es accesible directamente y esa cabecera se podría
      // falsificar, así que no sirve para limitar por IP (pregunta abierta del informe de F2-2).
      const apiRequestPolicy = new OriginRequestPolicy(this, 'ApiOriginRequest', {
        originRequestPolicyName: `${config.prefix}-web-api`,
        comment: 'Borrado por web hacia la API (F1-2).',
        headerBehavior: OriginRequestHeaderBehavior.allowList('Content-Type'),
        cookieBehavior: OriginRequestCookieBehavior.none(),
        queryStringBehavior: OriginRequestQueryStringBehavior.none(),
      });
      apiBehaviors['/v1/account/delete-*'] = {
        origin: apiOrigin,
        viewerProtocolPolicy: ViewerProtocolPolicy.HTTPS_ONLY,
        allowedMethods: AllowedMethods.ALLOW_ALL,
        cachePolicy: CachePolicy.CACHING_DISABLED,
        originRequestPolicy: apiRequestPolicy,
        responseHeadersPolicy: headers,
        compress: true,
      };
    }

    const domainNames = config.domain ? [config.domain.name, `www.${config.domain.name}`] : undefined;

    this.distribution = new Distribution(this, 'Distribution', {
      comment: `${config.prefix} web`,
      defaultRootObject: 'index.html',
      httpVersion: HttpVersion.HTTP2_AND_3,
      // Latinoamérica solo está en la clase de precio ALL.
      priceClass: PriceClass.PRICE_CLASS_ALL,
      // Solo tiene efecto con certificado propio (con el de *.cloudfront.net la política es fija).
      minimumProtocolVersion: config.domain ? SecurityPolicyProtocol.TLS_V1_2_2021 : undefined,
      domainNames,
      certificate: props.certificate,
      enableLogging: true,
      logBucket: logs,
      logFilePrefix: 'cloudfront/web/',
      defaultBehavior: {
        origin: S3BucketOrigin.withOriginAccessControl(site),
        viewerProtocolPolicy: ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
        allowedMethods: AllowedMethods.ALLOW_GET_HEAD,
        cachePolicy: CachePolicy.CACHING_OPTIMIZED,
        responseHeadersPolicy: headers,
        compress: true,
        functionAssociations: [{ function: indexRewrite, eventType: FunctionEventType.VIEWER_REQUEST }],
      },
      additionalBehaviors: apiBehaviors,
    });
    ack(this.distribution, 'AwsSolutions-CFR1', 'Juego global (Latinoamérica primero): sin restricción geográfica.');
    ack(this.distribution, 'AwsSolutions-CFR2', 'Sin WAF en la 1.0 (Q-AWS-10): límites de API Gateway y en DynamoDB; WAF solo si hay abuso.');
    if (!config.domain) {
      // Con el certificado por defecto de *.cloudfront.net CloudFront no permite fijar la versión mínima de TLS.
      ack(this.distribution, 'AwsSolutions-CFR4', 'Sin dominio propio (Q-AWS-9) se usa el certificado *.cloudfront.net, que no admite fijar TLS 1.2; con config.domain se aplica TLSv1.2_2021.');
    }

    new CfnOutput(this, 'WebUrl', { description: 'URL de la web (CloudFront).', value: `https://${this.distribution.distributionDomainName}` });

    const deploymentLogs = new LogGroup(this, 'SiteDeploymentLogs', {
      retention: RetentionDays.ONE_MONTH,
      removalPolicy: RemovalPolicy.DESTROY,
    });
    const deployment = new BucketDeployment(this, 'SiteDeployment', {
      destinationBucket: site,
      sources: [
        // robots.txt, canonical y hreflang salen del generador según el entorno y el dominio (lib/web-site.ts).
        Source.asset(deployableSiteDir(config)),
        Source.jsonData(WEB_RUNTIME_CONFIG_FILE, webRuntimeConfig(config, props.apiId !== undefined)),
      ],
      prune: true,
      // Sin invalidación (evita dar a la Lambda permisos de CloudFront sobre `*`): con max-age de
      // 5 minutos los cambios llegan solos en ese tiempo.
      cacheControl: [CacheControl.setPublic(), CacheControl.maxAge(Duration.minutes(5))],
      memoryLimit: 256,
      logGroup: deploymentLogs,
    });
    acknowledgeBucketDeployment(this, deployment, site, config);

    if (config.domain?.hostedZoneId !== undefined) {
      const zone = HostedZone.fromHostedZoneAttributes(this, 'Zone', {
        hostedZoneId: config.domain.hostedZoneId,
        zoneName: config.domain.name,
      });
      const target = RecordTarget.fromAlias(new CloudFrontTarget(this.distribution));
      for (const name of [config.domain.name, `www.${config.domain.name}`]) {
        const suffix = name === config.domain.name ? 'Apex' : 'Www';
        new ARecord(this, `Alias${suffix}`, { zone, recordName: name, target });
        new AaaaRecord(this, `Alias${suffix}V6`, { zone, recordName: name, target });
      }
    } else if (config.domain) {
      // DNS externo (Cloudflare, D-044): ningún registro en AWS; el dueño crea estos dos CNAME sin proxy.
      new CfnOutput(this, 'WebDnsRecords', {
        description: 'Registros del DNS externo para la web (CNAME sin proxy; en el apex, CNAME aplanado).',
        value: Fn.join('', [
          `${config.domain.name} CNAME `,
          this.distribution.distributionDomainName,
          ` ; www.${config.domain.name} CNAME `,
          this.distribution.distributionDomainName,
        ]),
      });
    }
  }
}

/** Excepciones de cdk-nag de la Lambda que CDK usa para subir el sitio (BucketDeployment). */
function acknowledgeBucketDeployment(stack: Stack, deployment: BucketDeployment, site: Bucket, config: AfConfig): void {
  void deployment;
  const reason =
    'Lambda interna de CDK (BucketDeployment) que solo corre al desplegar: lee el zip del sitio del bucket de assets de CDK y sincroniza el bucket de la web (prune). Su política la genera aws-cdk-lib y se limita a esos dos buckets.';
  const assets = `arn:${stack.partition === 'aws' ? 'aws' : '<AWS::Partition>'}:s3:::cdk-hnb659fds-assets-${config.account ?? '<AWS::AccountId>'}-${config.region}/*`;
  const findings = [
    'AwsSolutions-IAM5[Action::s3:GetBucket*]',
    'AwsSolutions-IAM5[Action::s3:GetObject*]',
    'AwsSolutions-IAM5[Action::s3:List*]',
    'AwsSolutions-IAM5[Action::s3:Abort*]',
    'AwsSolutions-IAM5[Action::s3:DeleteObject*]',
    `AwsSolutions-IAM5[Resource::${assets}]`,
    `AwsSolutions-IAM5[Resource::<${stack.getLogicalId(site.node.defaultChild as CfnBucket)}.Arn>/*]`,
  ];
  for (const child of stack.node.children) {
    if (child.node.id.startsWith('Custom::CDKBucketDeployment')) {
      for (const id of findings) ack(child, id, reason);
      ack(child, 'AwsSolutions-IAM4[Policy::arn:<AWS::Partition>:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole]', `${reason} Su rol usa la política gestionada básica de Lambda y no es configurable.`);
      ack(child, 'AwsSolutions-L1', 'Runtime de la Lambda interna de CDK (BucketDeployment), fijado por aws-cdk-lib; se actualiza al subir aws-cdk-lib.');
    }
  }
}
