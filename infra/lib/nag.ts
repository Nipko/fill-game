// cdk-nag 3: las reglas se registran como plugin de validación de CDK y cualquier hallazgo
// (error o aviso) hace fallar la síntesis. Cada excepción se reconoce aquí con su motivo.
import { Validations } from 'aws-cdk-lib';
import type { IConstruct } from 'constructs';

/** Reconoce (suprime) una regla de cdk-nag sobre un construct, con su justificación. */
export function ack(scope: IConstruct, id: string, reason: string): void {
  Validations.of(scope).acknowledge({ id, reason });
}
