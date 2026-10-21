// Reglas transversales: cdk-nag sin hallazgos, nada público, ninguna cuenta ni secreto en las plantillas.
import { AwsSolutionsChecks } from 'cdk-nag';
import { describe, expect, it } from 'vitest';
import { allTemplates, build, DEV, PROD, PROD_DOMAIN, resourcesOfType } from '../helpers.ts';

describe.each([
  ['dev', DEV],
  ['prod', PROD],
  ['prod con dominio', PROD_DOMAIN],
])('seguridad (%s)', (_name, ctx) => {
  const b = build(ctx);

  it('cdk-nag (AwsSolutions) sin hallazgos sin reconocer', () => {
    const report = new AwsSolutionsChecks().validateScope(b.app);
    const lines = report.violations.map((v) => `${v.ruleName}: ${v.violatingResources.map((r) => r.constructPath).join(', ')}`);
    expect(lines).toEqual([]);
    expect(report.success).toBe(true);
  });

  it('ningún recurso público: sin Function URL, sin políticas con Principal "*" que permitan', () => {
    for (const { name, json } of allTemplates(b)) {
      expect(resourcesOfType(json, 'AWS::Lambda::Url'), name).toHaveLength(0);
      const text = JSON.stringify(json);
      expect(text, name).not.toMatch(/"PublicRead"|"public-read"/);
      for (const type of ['AWS::S3::BucketPolicy', 'AWS::SNS::TopicPolicy', 'AWS::SQS::QueuePolicy']) {
        for (const [id, p] of resourcesOfType(json, type)) {
          for (const s of (p.PolicyDocument as { Statement: Array<{ Effect: string; Principal?: unknown }> }).Statement) {
            if (s.Effect === 'Allow') expect(JSON.stringify(s.Principal), `${name}/${id}`).not.toMatch(/^"\*"$|"AWS":"\*"/);
          }
        }
      }
    }
  });

  it('logs con 30 días de retención en todos los log groups', () => {
    for (const { json } of allTemplates(b)) {
      for (const [id, p] of resourcesOfType(json, 'AWS::Logs::LogGroup')) expect(p.RetentionInDays, id).toBe(30);
    }
  });

  it('ningún nombre de recurso lleva la marca', () => {
    for (const { name, json } of allTemplates(b)) {
      expect(JSON.stringify(json).toLowerCase(), name).not.toMatch(/tintal/);
    }
  });
});

describe('sin cuentas ni secretos en las plantillas', () => {
  it('sin -c account no aparece ningún id de cuenta de 12 dígitos', () => {
    for (const ctx of [DEV, PROD]) {
      for (const { name, json } of allTemplates(build(ctx))) {
        expect(JSON.stringify(json), name).not.toMatch(/(?<![0-9])\d{12}(?![0-9])/);
      }
    }
  });

  it('ningún valor de secreto en claro', () => {
    for (const ctx of [DEV, PROD, PROD_DOMAIN]) {
      for (const { name, json } of allTemplates(build(ctx))) {
        const text = JSON.stringify(json);
        expect(text, name).not.toMatch(/"SecretString"|BEGIN PRIVATE KEY|private_key/);
      }
    }
  });

  it('la cuenta del contexto solo se usa en el modo con dominio', () => {
    const b = build(PROD_DOMAIN);
    expect(b.stacks.api.account).toBe('123456789012');
    expect(build(DEV).stacks.api.account).toMatch(/^\$\{Token/);
  });
});
