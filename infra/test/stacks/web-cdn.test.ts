import { Match } from 'aws-cdk-lib/assertions';
import type { Template } from 'aws-cdk-lib/assertions';
import { readFileSync } from 'node:fs';
import * as path from 'node:path';
import { describe, expect, it } from 'vitest';
import { WEB_SITE_DIR } from '../../lib/paths.ts';
import { deployableSiteDir } from '../../lib/web-site.ts';
import { WEB_CSP, webRuntimeConfig } from '../../lib/web-stack.ts';
import {
  build,
  DEV,
  PROD,
  PROD_DOMAIN,
  PROD_EXTERNAL_DNS,
  PROD_EXTERNAL_DNS_EXTRAS,
  PROD_WEB_ONLY,
  resourcesOfType,
} from '../helpers.ts';

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

  it('el borrado por web llega a la API solo con Content-Type (sin cabeceras de IP falsificables)', () => {
    const policies = resourcesOfType(json, 'AWS::CloudFront::OriginRequestPolicy');
    expect(policies).toHaveLength(1);
    const cfg = policies[0][1].OriginRequestPolicyConfig as Record<string, Record<string, unknown>>;
    expect(cfg.HeadersConfig).toEqual({ HeaderBehavior: 'whitelist', Headers: ['Content-Type'] });
    expect(cfg.CookiesConfig).toEqual({ CookieBehavior: 'none' });
    expect(cfg.QueryStringsConfig).toEqual({ QueryStringBehavior: 'none' });
  });

  it('cabeceras de seguridad y noindex sin dominio (dev y prod en *.cloudfront.net)', () => {
    const policy = resourcesOfType(json, 'AWS::CloudFront::ResponseHeadersPolicy')[0][1].ResponseHeadersPolicyConfig as Record<string, unknown>;
    const sec = policy.SecurityHeadersConfig as Record<string, Record<string, unknown>>;
    expect(sec.ContentSecurityPolicy.ContentSecurityPolicy).toMatch(/default-src 'self'/);
    expect(sec.ContentSecurityPolicy.ContentSecurityPolicy).toMatch(/frame-ancestors 'none'/);
    expect(sec.StrictTransportSecurity).toMatchObject({ AccessControlMaxAgeSec: 63072000, IncludeSubdomains: true });
    expect(sec.FrameOptions).toMatchObject({ FrameOption: 'DENY' });
    expect(sec.ContentTypeOptions).toBeDefined();
    const custom = (policy.CustomHeadersConfig as { Items: Array<{ Header: string; Value: string }> }).Items;
    const robots = custom.find((h) => h.Header === 'X-Robots-Tag');
    expect(robots?.Value, envName).toBe('noindex, nofollow');
  });

  it('despliega el sitio (con robots.txt) y config.json con el borrado activo (mismo origen)', () => {
    const deployments = resourcesOfType(json, 'Custom::CDKBucketDeployment');
    expect(deployments).toHaveLength(1);
    expect(deployments[0][1].SourceObjectKeys as unknown[]).toHaveLength(2);
    expect(webRuntimeConfig(build(ctx).config, true)).toEqual({ deleteEnabled: true, apiBase: '' });
    expect(deployableSiteDir(build(ctx).config)).toBe(WEB_SITE_DIR);
  });

  it('la CSP deja la web sin terceros: scripts, estilos y conexiones solo del mismo origen', () => {
    for (const d of ["default-src 'self'", "script-src 'self'", "style-src 'self'", "connect-src 'self'", "object-src 'none'", "form-action 'self'", "frame-ancestors 'none'"]) {
      expect(WEB_CSP).toContain(d);
    }
    expect(WEB_CSP).not.toMatch(/unsafe-inline|unsafe-eval|https?:|\*/);
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
    // Un solo certificado, compartido por la web y la CDN (la vía de Route 53 no cambia).
    t.global.resourceCountIs('AWS::CertificateManager::Certificate', 1);
  });
});

function distConfig(tpl: Template): Record<string, unknown> {
  return resourcesOfType(tpl.toJSON(), 'AWS::CloudFront::Distribution')[0][1].DistributionConfig as Record<string, unknown>;
}

type Outputs = Record<string, { Value: unknown; Description?: string }>;

function outputs(tpl: Template): Outputs {
  return (tpl.toJSON() as { Outputs?: Outputs }).Outputs ?? {};
}

describe('dominio con DNS externo (Cloudflare, D-044)', () => {
  const b = build(PROD_EXTERNAL_DNS);
  const { t } = b;

  it('certificado de us-east-1 para el dominio y www., validado por DNS sin zona', () => {
    expect(b.stacks.global.region).toBe('us-east-1');
    t.global.resourceCountIs('AWS::CertificateManager::Certificate', 1);
    const [[, cert]] = resourcesOfType(t.global.toJSON(), 'AWS::CertificateManager::Certificate');
    expect(cert.DomainName).toBe('example.com');
    expect(cert.SubjectAlternativeNames).toEqual(['www.example.com']);
    expect(cert.ValidationMethod).toBe('DNS');
    // Sin zona: ni HostedZoneId en las opciones de validación ni registros.
    expect(JSON.stringify(cert)).not.toMatch(/HostedZoneId/);
  });

  it('ningún registro DNS ni zona en AWS', () => {
    for (const [name, tpl] of Object.entries(t)) {
      expect(JSON.stringify(tpl.toJSON()), name).not.toMatch(/AWS::Route53::/);
    }
  });

  it('salidas para el dueño: dónde leer los CNAME de validación y a qué apuntar el dominio', () => {
    const g = outputs(t.global);
    expect(Object.keys(g)).toEqual(expect.arrayContaining(['WebCertificateArn', 'WebCertificateValidation']));
    expect(JSON.stringify(g.WebCertificateValidation.Value)).toMatch(/aws acm describe-certificate --region us-east-1/);
    expect(g.WebCertificateValidation.Description).toMatch(/sin proxy/);
    const w = outputs(t.web);
    expect(JSON.stringify(w.WebDnsRecords.Value)).toMatch(/example\.com CNAME .*www\.example\.com CNAME /);
  });

  it('CloudFront de la web con los nombres alternativos, el certificado y TLS 1.2', () => {
    const dist = distConfig(t.web);
    expect(dist.Aliases).toEqual(['example.com', 'www.example.com']);
    expect(dist.ViewerCertificate).toMatchObject({ MinimumProtocolVersion: 'TLSv1.2_2021', SslSupportMethod: 'sni-only' });
    expect(JSON.stringify(dist.ViewerCertificate)).toMatch(/AcmCertificateArn/);
  });

  it('cdn. y api. no van en el dominio si no se piden', () => {
    expect(distConfig(t.cdn).Aliases).toBeUndefined();
    t.api.resourceCountIs('AWS::ApiGatewayV2::DomainName', 0);
    t.api.resourceCountIs('AWS::CertificateManager::Certificate', 0);
  });

  it('prod con dominio se indexa: sin X-Robots-Tag y con el sitio de canonical absoluto', () => {
    const policy = resourcesOfType(t.web.toJSON(), 'AWS::CloudFront::ResponseHeadersPolicy')[0][1]
      .ResponseHeadersPolicyConfig as Record<string, unknown>;
    const custom = (policy.CustomHeadersConfig as { Items: Array<{ Header: string }> }).Items;
    expect(custom.find((h) => h.Header === 'X-Robots-Tag')).toBeUndefined();
    const dir = deployableSiteDir(b.config);
    expect(dir).not.toBe(WEB_SITE_DIR);
    expect(readFileSync(path.join(dir, 'robots.txt'), 'utf8')).toBe('User-agent: *\nAllow: /\n');
    expect(readFileSync(path.join(dir, 'index.html'), 'utf8')).toContain('<link rel="canonical" href="https://example.com/">');
  });
});

describe('dominio con DNS externo, con cdn. y api. pedidos', () => {
  const { t } = build(PROD_EXTERNAL_DNS_EXTRAS);

  it('certificado propio para cdn. (no reemplaza el de la web) y alias en la CDN', () => {
    const certs = resourcesOfType(t.global.toJSON(), 'AWS::CertificateManager::Certificate').map(([, p]) => p);
    expect(certs.map((c) => c.DomainName).sort()).toEqual(['cdn.example.com', 'example.com']);
    const dist = distConfig(t.cdn);
    expect(dist.Aliases).toEqual(['cdn.example.com']);
    expect(dist.ViewerCertificate).toMatchObject({ MinimumProtocolVersion: 'TLSv1.2_2021' });
    expect(JSON.stringify(outputs(t.cdn).CdnDnsRecord.Value)).toMatch(/cdn\.example\.com CNAME /);
  });

  it('api. con certificado regional validado sin zona y la salida del CNAME', () => {
    t.api.hasResourceProperties('AWS::ApiGatewayV2::DomainName', { DomainName: 'api.example.com' });
    const [[, cert]] = resourcesOfType(t.api.toJSON(), 'AWS::CertificateManager::Certificate');
    expect(cert).toMatchObject({ DomainName: 'api.example.com', ValidationMethod: 'DNS' });
    expect(JSON.stringify(cert)).not.toMatch(/HostedZoneId/);
    const o = outputs(t.api);
    expect(Object.keys(o)).toEqual(expect.arrayContaining(['ApiCertificateArn', 'ApiCertificateValidation', 'ApiDnsRecord']));
    expect(JSON.stringify(o.ApiCertificateValidation.Value)).toMatch(/--region sa-east-1/);
    for (const [name, tpl] of Object.entries(t)) expect(JSON.stringify(tpl.toJSON()), name).not.toMatch(/AWS::Route53::/);
  });
});

describe('prod solo web (D-044)', () => {
  const b = build(PROD_WEB_ONLY);

  it('solo Global y Web: sin Data, Api, Cdn ni Ops, y sin firebaseAppIds', () => {
    expect(Object.keys(b.stacks).sort()).toEqual(['global', 'web']);
    expect(b.app.node.children.map((c) => c.node.id).filter((id) => id.startsWith('Af')).sort()).toEqual(['AfProdGlobal', 'AfProdWeb']);
    expect(b.config.firebaseAppIds).toEqual([]);
    // La web solo depende del stack global (el certificado).
    expect(b.stacks.web.dependencies.map((d) => d.stackName)).toEqual(['AfProdGlobal']);
  });

  it('sin la ruta del borrado hacia la API y con el borrado web desactivado en config.json', () => {
    const json = b.t.web.toJSON();
    const dist = distConfig(b.t.web);
    expect(dist.CacheBehaviors).toBeUndefined();
    expect((dist.Origins as Array<Record<string, unknown>>).some((o) => o.CustomOriginConfig)).toBe(false);
    expect(resourcesOfType(json, 'AWS::CloudFront::OriginRequestPolicy')).toHaveLength(0);
    expect(JSON.stringify(json)).not.toMatch(/execute-api/);
    expect(webRuntimeConfig(b.config, false)).toEqual({ deleteEnabled: false });
    expect(resourcesOfType(json, 'Custom::CDKBucketDeployment')[0][1].SourceObjectKeys as unknown[]).toHaveLength(2);
  });

  it('con el certificado del dominio, los alias y sin registros DNS', () => {
    expect(distConfig(b.t.web).Aliases).toEqual(['example.com', 'www.example.com']);
    b.t.global.resourceCountIs('AWS::CertificateManager::Certificate', 1);
    for (const [name, tpl] of Object.entries(b.t)) expect(JSON.stringify(tpl.toJSON()), name).not.toMatch(/AWS::Route53::/);
  });

  it('dev solo web tampoco tiene API ni se indexa', () => {
    const dev = build({ env: 'dev', webOnly: 'true' });
    expect(Object.keys(dev.stacks).sort()).toEqual(['global', 'web']);
    expect(distConfig(dev.t.web).CacheBehaviors).toBeUndefined();
    expect(deployableSiteDir(dev.config)).toBe(WEB_SITE_DIR);
  });
});
