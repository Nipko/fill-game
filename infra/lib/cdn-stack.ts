// F1-4 · cdn-stack: contenido remoto (levels_remote y catálogo; Q-AWS-8: el arte de los mundos
// sigue en Play Asset Delivery). Bucket af-<env>-content privado tras CloudFront con OAC:
// packs/<content_version>/… inmutables con caché larga, catalog.json con TTL de 5 minutos,
// sin listado (sin s3:ListBucket) y sin CORS (la app no lo necesita).
import { CfnOutput, Duration, Stack } from 'aws-cdk-lib';
import type { StackProps } from 'aws-cdk-lib';
import type { ICertificate } from 'aws-cdk-lib/aws-certificatemanager';
import {
  AllowedMethods,
  CacheCookieBehavior,
  CacheHeaderBehavior,
  CachePolicy,
  CacheQueryStringBehavior,
  Distribution,
  HeadersFrameOption,
  HeadersReferrerPolicy,
  HttpVersion,
  PriceClass,
  ResponseHeadersPolicy,
  SecurityPolicyProtocol,
  ViewerProtocolPolicy,
} from 'aws-cdk-lib/aws-cloudfront';
import type { IResponseHeadersPolicy } from 'aws-cdk-lib/aws-cloudfront';
import { S3BucketOrigin } from 'aws-cdk-lib/aws-cloudfront-origins';
import { ARecord, AaaaRecord, HostedZone, RecordTarget } from 'aws-cdk-lib/aws-route53';
import { CloudFrontTarget } from 'aws-cdk-lib/aws-route53-targets';
import type { Construct } from 'constructs';
import type { AfConfig } from './config.ts';
import { AccessLogsBucket, privateBucket } from './constructs/private-bucket.ts';
import { ack } from './nag.ts';

export interface CdnStackProps extends StackProps {
  readonly config: AfConfig;
  readonly certificate?: ICertificate;
}

/** TTL de catalog.json (plan §2). */
export const CATALOG_TTL = Duration.minutes(5);
/** Caché de los packs inmutables: un año. */
export const PACK_TTL = Duration.days(365);

export class CdnStack extends Stack {
  readonly distribution: Distribution;

  constructor(scope: Construct, id: string, props: CdnStackProps) {
    super(scope, id, props);
    const { config } = props;

    const logs = new AccessLogsBucket(this, 'AccessLogs', config).bucket;
    const content = privateBucket(this, 'ContentBucket', {
      config,
      logs,
      logPrefix: 's3/content/',
      bucketName: `${config.prefix}-content-${this.account}`,
      // Versionado para poder volver a un catalog.json anterior; las versiones viejas caducan.
      versioned: true,
      lifecycleRules: [
        { id: 'old-versions', noncurrentVersionExpiration: Duration.days(30), abortIncompleteMultipartUploadAfter: Duration.days(1) },
      ],
    });

    // Sin cookies, cabeceras ni query en la clave de caché: cada URL es un objeto.
    const cachePolicy = (name: string, ttl: Duration): CachePolicy =>
      new CachePolicy(this, `${name}CachePolicy`, {
        cachePolicyName: `${config.prefix}-cdn-${name.toLowerCase()}`,
        defaultTtl: ttl,
        minTtl: ttl,
        maxTtl: ttl,
        cookieBehavior: CacheCookieBehavior.none(),
        headerBehavior: CacheHeaderBehavior.none(),
        queryStringBehavior: CacheQueryStringBehavior.none(),
        enableAcceptEncodingGzip: true,
        enableAcceptEncodingBrotli: true,
      });
    const packCache = cachePolicy('Packs', PACK_TTL);
    const catalogCache = cachePolicy('Catalog', CATALOG_TTL);

    const headersPolicy = (name: string, cacheControl: string): IResponseHeadersPolicy =>
      new ResponseHeadersPolicy(this, `${name}Headers`, {
        responseHeadersPolicyName: `${config.prefix}-cdn-${name.toLowerCase()}`,
        comment: `Cabeceras de la CDN (${name}).`,
        securityHeadersBehavior: {
          contentTypeOptions: { override: true },
          frameOptions: { frameOption: HeadersFrameOption.DENY, override: true },
          referrerPolicy: { referrerPolicy: HeadersReferrerPolicy.NO_REFERRER, override: true },
          strictTransportSecurity: { accessControlMaxAge: Duration.days(730), includeSubdomains: true, override: true },
        },
        customHeadersBehavior: {
          customHeaders: [
            { header: 'Cache-Control', value: cacheControl, override: true },
            { header: 'X-Robots-Tag', value: 'noindex, nofollow', override: true },
          ],
        },
        // Sin corsBehavior: no se emite ninguna cabecera Access-Control-*.
      });
    const packHeaders = headersPolicy('Packs', `public, max-age=${PACK_TTL.toSeconds()}, immutable`);
    const catalogHeaders = headersPolicy('Catalog', `public, max-age=${CATALOG_TTL.toSeconds()}`);

    const origin = S3BucketOrigin.withOriginAccessControl(content);
    // cdn.<dominio>: con Route 53, siempre; con DNS externo, solo con domainExtras=cdn (D-044).
    const cdnName = config.domain?.cdn ? `cdn.${config.domain.name}` : undefined;
    const domainNames = cdnName ? [cdnName] : undefined;

    this.distribution = new Distribution(this, 'Distribution', {
      comment: `${config.prefix} cdn`,
      httpVersion: HttpVersion.HTTP2_AND_3,
      priceClass: PriceClass.PRICE_CLASS_ALL,
      // Solo tiene efecto con certificado propio (con el de *.cloudfront.net la política es fija).
      minimumProtocolVersion: cdnName ? SecurityPolicyProtocol.TLS_V1_2_2021 : undefined,
      domainNames,
      certificate: cdnName ? props.certificate : undefined,
      enableLogging: true,
      logBucket: logs,
      logFilePrefix: 'cloudfront/cdn/',
      // Lo que no es un pack se trata como el catálogo: caché corta.
      defaultBehavior: {
        origin,
        viewerProtocolPolicy: ViewerProtocolPolicy.HTTPS_ONLY,
        allowedMethods: AllowedMethods.ALLOW_GET_HEAD,
        cachePolicy: catalogCache,
        responseHeadersPolicy: catalogHeaders,
        compress: true,
      },
      additionalBehaviors: {
        'catalog.json': {
          origin,
          viewerProtocolPolicy: ViewerProtocolPolicy.HTTPS_ONLY,
          allowedMethods: AllowedMethods.ALLOW_GET_HEAD,
          cachePolicy: catalogCache,
          responseHeadersPolicy: catalogHeaders,
          compress: true,
        },
        'packs/*': {
          origin,
          viewerProtocolPolicy: ViewerProtocolPolicy.HTTPS_ONLY,
          allowedMethods: AllowedMethods.ALLOW_GET_HEAD,
          cachePolicy: packCache,
          responseHeadersPolicy: packHeaders,
          compress: true,
        },
      },
    });
    ack(this.distribution, 'AwsSolutions-CFR1', 'Contenido del juego para todos los países: sin restricción geográfica.');
    ack(this.distribution, 'AwsSolutions-CFR2', 'Contenido público de solo lectura, sin WAF en la 1.0 (Q-AWS-10).');
    if (!cdnName) {
      ack(this.distribution, 'AwsSolutions-CFR4', 'Sin cdn.<dominio> (Q-AWS-9; con DNS externo solo con domainExtras=cdn) se usa el certificado *.cloudfront.net, que no admite fijar TLS 1.2; con cdn.<dominio> se aplica TLSv1.2_2021.');
    }

    new CfnOutput(this, 'CdnUrl', { description: 'URL de la CDN (CloudFront).', value: `https://${this.distribution.distributionDomainName}` });
    new CfnOutput(this, 'ContentBucketName', { description: 'Bucket donde se suben packs/ y catalog.json.', value: content.bucketName });

    if (cdnName && config.domain?.hostedZoneId !== undefined) {
      const zone = HostedZone.fromHostedZoneAttributes(this, 'Zone', {
        hostedZoneId: config.domain.hostedZoneId,
        zoneName: config.domain.name,
      });
      const target = RecordTarget.fromAlias(new CloudFrontTarget(this.distribution));
      new ARecord(this, 'Alias', { zone, recordName: cdnName, target });
      new AaaaRecord(this, 'AliasV6', { zone, recordName: cdnName, target });
    } else if (cdnName) {
      new CfnOutput(this, 'CdnDnsRecord', {
        description: `Registro del DNS externo para ${cdnName} (sin proxy).`,
        value: `${cdnName} CNAME ${this.distribution.distributionDomainName}`,
      });
    }
  }
}
