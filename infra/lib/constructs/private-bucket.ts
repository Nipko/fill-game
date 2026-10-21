// Buckets S3 del backend: siempre privados, cifrados, solo por TLS y con logs de acceso.
import { Duration, RemovalPolicy } from 'aws-cdk-lib';
import {
  BlockPublicAccess,
  Bucket,
  BucketEncryption,
  ObjectOwnership,
} from 'aws-cdk-lib/aws-s3';
import type { BucketProps } from 'aws-cdk-lib/aws-s3';
import { Construct } from 'constructs';
import type { AfConfig } from '../config.ts';
import { ack } from '../nag.ts';

const SECURE_DEFAULTS = {
  blockPublicAccess: BlockPublicAccess.BLOCK_ALL,
  encryption: BucketEncryption.S3_MANAGED,
  enforceSSL: true,
  minimumTLSVersion: 1.2,
} as const;

/**
 * Bucket de logs de acceso (S3 y CloudFront). Los logs llevan IP, que es dato personal (plan §5):
 * caducan a los 30 días. CloudFront escribe con ACL, de ahí `BUCKET_OWNER_PREFERRED`.
 */
export class AccessLogsBucket extends Construct {
  readonly bucket: Bucket;

  constructor(scope: Construct, id: string, config: AfConfig) {
    super(scope, id);
    this.bucket = new Bucket(this, 'Bucket', {
      ...SECURE_DEFAULTS,
      objectOwnership: ObjectOwnership.BUCKET_OWNER_PREFERRED,
      lifecycleRules: [
        { id: 'expire-logs', expiration: Duration.days(config.logRetentionDays), abortIncompleteMultipartUploadAfter: Duration.days(1) },
      ],
      removalPolicy: config.isProd ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY,
      autoDeleteObjects: false,
    });
    ack(this.bucket, 'AwsSolutions-S1', 'Es el propio bucket de destino de los logs de acceso; registrarlo en sí mismo crearía un bucle.');
  }
}

export interface PrivateBucketProps {
  readonly config: AfConfig;
  readonly logs: Bucket;
  readonly logPrefix: string;
  readonly bucketName?: string;
  readonly versioned?: boolean;
  readonly lifecycleRules?: BucketProps['lifecycleRules'];
}

/** Bucket de contenido privado: solo CloudFront (OAC) puede leerlo. */
export function privateBucket(scope: Construct, id: string, props: PrivateBucketProps): Bucket {
  return new Bucket(scope, id, {
    ...SECURE_DEFAULTS,
    bucketName: props.bucketName,
    objectOwnership: ObjectOwnership.BUCKET_OWNER_ENFORCED,
    versioned: props.versioned ?? false,
    serverAccessLogsBucket: props.logs,
    serverAccessLogsPrefix: props.logPrefix,
    lifecycleRules: props.lifecycleRules,
    removalPolicy: props.config.isProd ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY,
    autoDeleteObjects: false,
  });
}
