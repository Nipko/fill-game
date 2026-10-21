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

/** Segundos epoch (atributo `ttl` de DynamoDB). */
export const epochS = (ms: number): number => Math.floor(ms / 1000);
