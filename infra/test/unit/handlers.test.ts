import { describe, expect, it } from 'vitest';
import type { APIGatewayProxyStructuredResultV2 } from 'aws-lambda';
import { handler as deleteAccount } from '../../functions/delete-account/handler.ts';
import { handler as restoreLedger } from '../../functions/restore-ledger/handler.ts';
import { handler as rewardedSsv } from '../../functions/rewarded-ssv/handler.ts';
import { handler as verifyPurchase } from '../../functions/verify-purchase/handler.ts';
import { handler as voidedJob } from '../../functions/voided-purchases-job/handler.ts';
import { validator } from '../contract/ajv.ts';

describe('esqueletos de F2-2', () => {
  const http = { verifyPurchase, restoreLedger, rewardedSsv, deleteAccount };

  for (const [name, h] of Object.entries(http)) {
    it(`${name} responde 501 con el error del contrato`, async () => {
      const res = (await h()) as APIGatewayProxyStructuredResultV2;
      expect(res.statusCode).toBe(501);
      expect(res.headers?.['content-type']).toMatch(/application\/json/);
      expect(res.headers?.['cache-control']).toBe('no-store');
      const body = JSON.parse(res.body ?? '') as unknown;
      const validate = validator('error.schema.json');
      expect(validate(body), JSON.stringify(validate.errors)).toBe(true);
      expect(body).toMatchObject({ error: { code: 'not_implemented' } });
    });
  }

  it('voidedPurchasesJob no lanza y devuelve not_implemented según el contrato', async () => {
    const out = await voidedJob();
    const validate = validator('voided-purchases-job.output.schema.json');
    expect(validate(out), JSON.stringify(validate.errors)).toBe(true);
    expect(out.status).toBe('not_implemented');
  });
});
