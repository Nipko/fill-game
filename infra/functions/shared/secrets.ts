// Lectura de secretos de Secrets Manager con caché corta (los carga el dueño con put-secret-value;
// infra/README.md). El valor nunca se registra.
import { GetSecretValueCommand, SecretsManagerClient } from '@aws-sdk/client-secrets-manager';
import { ConfigError, UpstreamError } from './errors.ts';

export interface SecretSource {
  /** Valor de texto del secreto `arn`. */
  get(arn: string): Promise<string>;
}

const TTL_MS = 5 * 60_000;

export class SecretsManagerSource implements SecretSource {
  private readonly cache = new Map<string, { value: string; until: number }>();
  private readonly client: SecretsManagerClient;
  private readonly now: () => number;

  constructor(client?: SecretsManagerClient, now: () => number = Date.now) {
    this.client = client ?? new SecretsManagerClient({ maxAttempts: 3 });
    this.now = now;
  }

  async get(arn: string): Promise<string> {
    const hit = this.cache.get(arn);
    if (hit && hit.until > this.now()) return hit.value;
    let value: string | undefined;
    try {
      const out = await this.client.send(new GetSecretValueCommand({ SecretId: arn }));
      value = out.SecretString;
    } catch (e) {
      throw new UpstreamError('secretsmanager', e instanceof Error ? e.name : 'error');
    }
    if (!value) throw new ConfigError('El secreto no tiene valor de texto.');
    this.cache.set(arn, { value, until: this.now() + TTL_MS });
    return value;
  }
}
