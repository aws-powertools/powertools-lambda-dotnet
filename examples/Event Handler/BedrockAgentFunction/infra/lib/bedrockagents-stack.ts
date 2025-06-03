import {
  Stack,
  type StackProps,
  CfnOutput,
  RemovalPolicy,
  Arn,
  Duration,
} from 'aws-cdk-lib';
import type { Construct } from 'constructs';
import { Runtime, Function as LambdaFunction, Code, Architecture } from 'aws-cdk-lib/aws-lambda';
import { LogGroup, RetentionDays } from 'aws-cdk-lib/aws-logs';
import { CfnAgent } from 'aws-cdk-lib/aws-bedrock';
import {
  Effect,
  PolicyDocument,
  PolicyStatement,
  Role,
  ServicePrincipal,
} from 'aws-cdk-lib/aws-iam';

export class BedrockAgentsStack extends Stack {
  constructor(scope: Construct, id: string, props?: StackProps) {
    super(scope, id, props);

    const fnName = 'BedrockAgentsFn';
    const logGroup = new LogGroup(this, 'MyLogGroup', {
      logGroupName: `/aws/lambda/${fnName}`,
      removalPolicy: RemovalPolicy.DESTROY,
      retention: RetentionDays.ONE_DAY,
    });

    const fn = new LambdaFunction(this, 'MyFunction', {
      functionName: fnName,
      logGroup,
      timeout: Duration.minutes(3),
      runtime: Runtime.DOTNET_8,
      handler: 'BedrockAgentFunction',
      code: Code.fromAsset('../release/BedrockAgentFunction.zip'),
      architecture: Architecture.X86_64,
    });

    const agentRole = new Role(this, 'MyAgentRole', {
      assumedBy: new ServicePrincipal('bedrock.amazonaws.com'),
      description: 'Role for Bedrock airport agent',
      inlinePolicies: {
        bedrock: new PolicyDocument({
          statements: [
            new PolicyStatement({
              actions: [
                'bedrock:*',
              ],
              resources: [
                Arn.format(
                  {
                    service: 'bedrock',
                    resource: 'foundation-model/*',
                    region: 'us-*',
                    account: '',
                  },
                  Stack.of(this)
                ),
                Arn.format(
                  {
                    service: 'bedrock',
                    resource: 'inference-profile/*',
                    region: 'us-*',
                    account: '*',
                  },
                  Stack.of(this)
                ),
              ],
            }),
          ],
        }),
      },
    });

    const agent = new CfnAgent(this, 'MyCfnAgent', {
      agentName: 'airportAgent',
      actionGroups: [
        {
          actionGroupName: 'airportActionGroup',
          actionGroupExecutor: {
            lambda: fn.functionArn,
          },
          functionSchema: {
            functions: [
              {
                name: 'getAirportCodeForCity',
                description: 'Get airport code and full airport name for a specific city',
                parameters: {
                  city: {
                    type: 'string',
                    description: 'The name of the city to get the airport code for',
                    required: true,
                  },
                },
              },
            ],
          },
        },
      ],
      agentResourceRoleArn: agentRole.roleArn,
      autoPrepare: true,
      description: 'A simple airport agent',
      foundationModel: `arn:aws:bedrock:us-west-2:${Stack.of(this).account}:inference-profile/us.amazon.nova-pro-v1:0`,
      instruction:
        'You are an airport traffic control agent. You will be given a city name and you will return the airport code and airport full name for that city.',
    });

    fn.addPermission('BedrockAgentInvokePermission', {
      principal: new ServicePrincipal('bedrock.amazonaws.com'),
      action: 'lambda:InvokeFunction',
      sourceAccount: this.account,
      sourceArn: `arn:aws:bedrock:${this.region}:${this.account}:agent/${agent.attrAgentId}`,
    });

    new CfnOutput(this, 'FunctionArn', {
      value: fn.functionArn,
    });
  }
}
