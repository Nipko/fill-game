import { Match } from 'aws-cdk-lib/assertions';
import type { Template } from 'aws-cdk-lib/assertions';
import { describe, expect, it } from 'vitest';
import { build, DEV, PROD, PROD_DOMAIN, resourcesOfType } from '../helpers.ts';

type Statement = { Effect: string; Principal?: unknown; Action: string | string[]; Condition?: unknown };

function expectPrivateBuckets(t: Template): void {
  const json = t.toJSON() as Record<string, unknown>;
  for (const [id, p] of resourcesOfType(json, 'AWS::S3::Bucket')) {
    expect(p.PublicAccessBlockConfiguration, id).toEqual({
      BlockPublicAcls: true,
      BlockPublicPolicy: true,
      IgnorePublicAcls: true,
      RestrictPublicBuckets: true,
    });
    expect(p.BucketEncryption, id).toBeDefined();
    expect(p.WebsiteConfiguration, id).toBeUndefined();
    expect(p.CorsConfiguration, id).toBeUndefined();
  }
  for (const [id, p] of resourcesOfType(json, 'AWS::S3::BucketPolicy')) {
    const statements = (p.PolicyDocument as { Statement: Statement[] }).Statement;
    // TLS obligatorio.
    expect(statements.some((s) => s.Effect === 'Deny' && JSON.stringify(s.Condition).includes('aws:SecureTransport')), id).toBe(true);
    for (const s of statements) {
      const principal = JSON.stringify(s.Principal);
      if (s.Effect === 'Allow') {
        // Nada público: solo CloudFront (OAC, con SourceArn) o el servicio de logs.
        expect(principal, id).not.toMatch(/^"\*"$|"AWS":"\*"/);
        expect(principal, id).toMatch(/cloudfront\.amazonaws\.com|logging\.s3\.amazonaws\.com/);
        const actions = Array.isArray(s.Action) ? s.Action : [s.Action];
        // Sin listado.
        expect(actions, id).not.toContain('s3:ListBucket');
      }
    }
  }
}

describe.each([
  ['dev', DEV],
  ['prod', PROD],
])('web-stack (%s)', (envName, ctx) => {
  const { t } = build(ctx);
  const json = t.web.toJSON() as Record<string, unknown>;

  it('buckets privados, cifrados, solo TLS y sin listado', () => expectPrivateBuckets(t.web));

  it('CloudFront con OAC, HTTPS y TLS 1.2 como mínimo', () => {
    t.web.resourceCountIs('AWS::CloudFront::OriginAccessControl', 1);
    t.web.resourceCountIs('AWS::CloudFront::CloudFrontOriginAccessIdentity', 0);
    const dist = resourcesOfType(json, 'AWS::CloudFront::Distribution')[0][1].DistributionConfig as Record<string, unknown>;
    expect(dist.DefaultRootObject).toBe('index.html');
    expect(dist.PriceClass).toBe('PriceClass_All');
    expect((dist.DefaultCacheBehavior as Record<string, unknown>).ViewerProtocolPolicy).toBe('redirect-to-https');
    expect(dist.Logging).toBeDefined();
    const api = (dist.CacheBehaviors as Array<Record<string, unknown>>).find((b) => b.PathPattern === '/v1/account/delete-*');
    expect(api?.ViewerProtocolPolicy).toBe('https-only');
    const origins = dist.Origins as Array<Record<string, unknown>>;
    const http = origins.find((o) => o.CustomOriginConfig);
    expect(http?.CustomOriginConfig).toMatchObject({ OriginProtocolPolicy: 'https-only', OriginSSLProtocols: ['TLSv1.2'] });
  });

  it('el borrado por web llega a la API solo con Content-Type y la IP del visitante', () => {
    const policies = resourcesOfType(json, 'AWS::CloudFront::OriginRequestPolicy');
    expect(policies).toHaveLength(1);
    const cfg = policies[0][1].OriginRequestPolicyConfig as Record<string, Record<string, unknown>>;
    expect(cfg.HeadersConfig).toEqual({ HeaderBehavior: 'whitelist', Headers: ['Content-Type', 'CloudFront-Viewer-Address'] });
    expect(cfg.CookiesConfig).toEqual({ CookieBehavior: 'none' });
    expect(cfg.QueryStringsConfig).toEqual({ QueryStringBehavior: 'none' });
  });

  it('cabeceras de seguridad y noindex solo en dev', () => {
    const policy = resourcesOfType(json, 'AWS::CloudFront::ResponseHeadersPolicy')[0][1].ResponseHeadersPolicyConfig as Record<string, unknown>;
    const sec = policy.SecurityHeadersConfig as Record<string, Record<string, unknown>>;
    expect(sec.ContentSecurityPolicy.ContentSecurityPolicy).toMatch(/default-src 'self'/);
    expect(sec.ContentSecurityPolicy.ContentSecurityPolicy).toMatch(/frame-ancestors 'none'/);
    expect(sec.StrictTransportSecurity).toMatchObject({ AccessControlMaxAgeSec: 63072000, IncludeSubdomains: true });
    expect(sec.FrameOptions).toMatchObject({ FrameOption: 'DENY' });
    expect(sec.ContentTypeOptions).toBeDefined();
    const custom = (policy.CustomHeadersConfig as { Items: Array<{ Header: string; Value: string }> }).Items;
    const robots = custom.find((h) => h.Header === 'X-Robots-Tag');
    if (envName === 'dev') expect(robots?.Value).toBe('noindex, nofollow');
    else expect(robots).toBeUndefined();
  });

  it('sin dominio no hay certificado, alias ni registros DNS', () => {
    const dist = resourcesOfType(json, 'AWS::CloudFront::Distribution')[0][1].DistributionConfig as Record<string, unknown>;
    expect(dist.Aliases).toBeUndefined();
    t.web.resourceCountIs('AWS::Route53::RecordSet', 0);
  });

  it('logs de acceso con caducidad de 30 días', () => {
    t.web.hasResourceProperties('AWS::S3::Bucket', {
      LifecycleConfiguration: { Rules: Match.arrayWith([Match.objectLike({ ExpirationInDays: 30, Status: 'Enabled' })]) },
    });
    for (const [, p] of resourcesOfType(json, 'AWS::Logs::LogGroup')) expect(p.RetentionInDays).toBe(30);
  });
});

describe.each([
  ['dev', DEV],
  ['prod', PROD],
])('cdn-stack (%s)', (envName, ctx) => {
  const { t } = build(ctx);
  const json = t.cdn.toJSON() as Record<string, unknown>;

  it('bucket af-<env>-content privado, sin listado ni CORS', () => {
    expectPrivateBuckets(t.cdn);
    const content = resourcesOfType(json, 'AWS::S3::Bucket').find(([id]) => id.startsWith('ContentBucket'));
    expect(JSON.stringify(content?.[1].BucketName)).toContain(`af-${envName}-content-`);
  });

  it('packs inmutables con caché de un año y catalog.json con 5 minutos', () => {
    const policies = resourcesOfType(json, 'AWS::CloudFront::CachePolicy').map(([, p]) => p.CachePolicyConfig as Record<string, number>);
    expect(policies.map((p) => p.DefaultTTL).sort((a, b) => a - b)).toEqual([300, 31536000]);
    const dist = resourcesOfType(json, 'AWS::CloudFront::Distribution')[0][1].DistributionConfig as Record<string, unknown>;
    const behaviors = (dist.CacheBehaviors as Array<Record<string, unknown>>).map((b) => b.PathPattern).sort();
    expect(behaviors).toEqual(['catalog.json', 'packs/*']);
    const headers = JSON.stringify(resourcesOfType(json, 'AWS::CloudFront::ResponseHeadersPolicy'));
    expect(headers).toContain('public, max-age=31536000, immutable');
    expect(headers).toContain('public, max-age=300');
    expect(headers).not.toMatch(/CorsConfig|Access-Control/);
    for (const b of [dist.DefaultCacheBehavior, ...(dist.CacheBehaviors as unknown[])] as Array<Record<string, unknown>>) {
      expect(b.AllowedMethods).toEqual(['GET', 'HEAD']);
      expect(b.ViewerProtocolPolicy).toBe('https-only');
    }
  });
});

describe('web y cdn con dominio (prod)', () => {
  const { t } = build(PROD_DOMAIN);

  it('TLS 1.2_2021, alias y certificado de us-east-1', () => {
    for (const tpl of [t.web, t.cdn]) {
      const dist = resourcesOfType(tpl.toJSON(), 'AWS::CloudFront::Distribution')[0][1]
        .DistributionConfig as Record<string, unknown>;
      expect(dist.ViewerCertificate).toMatchObject({ MinimumProtocolVersion: 'TLSv1.2_2021', SslSupportMethod: 'sni-only' });
    }
    t.global.hasResourceProperties('AWS::CertificateManager::Certificate', {
      DomainName: 'example.com',
      SubjectAlternativeNames: ['www.example.com', 'cdn.example.com'],
      ValidationMethod: 'DNS',
    });
    t.web.resourceCountIs('AWS::Route53::RecordSet', 4);
    t.cdn.resourceCountIs('AWS::Route53::RecordSet', 2);
  });
});
