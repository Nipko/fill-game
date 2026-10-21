// Cliente de DynamoDB (Document Client) compartido por las Lambdas. En los tests lo sustituye
// aws-sdk-client-mock (mockClient(DynamoDBDocumentClient)): nunca hay red.
import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { DynamoDBDocumentClient } from '@aws-sdk/lib-dynamodb';

let client: DynamoDBDocumentClient | undefined;

export function ddbClient(): DynamoDBDocumentClient {
  client ??= DynamoDBDocumentClient.from(new DynamoDBClient({ maxAttempts: 3 }), {
    marshallOptions: { removeUndefinedValues: true },
  });
  return client;
}

/** ¿Falló la condición de un PutItem/UpdateItem/DeleteItem condicional? */
export function isConditionalCheckFailed(e: unknown): boolean {
  return e instanceof Error && e.name === 'ConditionalCheckFailedException';
}

/**
 * Motivos de cancelación de un TransactWriteItems (uno por elemento, en el mismo orden: `ConditionalCheckFailed`,
 * `TransactionConflict`, `None`…). `undefined` si el error no es una transacción cancelada.
 */
export function cancellationCodes(e: unknown): string[] | undefined {
  if (!(e instanceof Error) || e.name !== 'TransactionCanceledException') return undefined;
  const reasons = (e as { CancellationReasons?: Array<{ Code?: string }> }).CancellationReasons ?? [];
  return reasons.map((r) => r.Code ?? 'None');
}

/** Segundos epoch (atributo `ttl` de DynamoDB). */
export const epochS = (ms: number): number => Math.floor(ms / 1000);
