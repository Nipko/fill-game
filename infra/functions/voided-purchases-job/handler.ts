// voidedPurchasesJob: esqueleto de F2-2. La lógica real llega en F2-7 (docs/dev/plan-backend-aws.md §9).
// No lanza error a propósito: un esqueleto que fallara llenaría la DLQ y dispararía la alarma cada día.

export interface JobResult {
  readonly status: 'ok' | 'not_implemented';
}

export const handler = (): Promise<JobResult> => {
  console.log(JSON.stringify({ msg: 'voidedPurchasesJob sin implementar (esqueleto F2-2)' }));
  return Promise.resolve({ status: 'not_implemented' });
};
