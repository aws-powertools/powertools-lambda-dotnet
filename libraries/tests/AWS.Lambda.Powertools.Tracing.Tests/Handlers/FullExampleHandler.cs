/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Tracing.Tests.Handlers;

public class FullExampleHandler
{
    [Tracing(Namespace = "ns", CaptureMode = TracingCaptureMode.ResponseAndError)]
    public async Task<string> Handle(string text, ILambdaContext context)
    {
        Tracing.AddAnnotation("annotation", "value");
        await BusinessLogic1();

        var response = DoAction(text);
        
        return await Task.FromResult(response);
    }

    [Tracing(SegmentName = "Do Action")]
    private string DoAction(string text)
    {
        return ToUpper(text);
    }

    [Tracing(SegmentName = "To Upper")]
    private string ToUpper(string text)
    {
        return text.ToUpper();
    }

    [Tracing(SegmentName = "First Call")]
    private async Task BusinessLogic1()
    {
        await BusinessLogic2();
    }

    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    private async Task BusinessLogic2()
    {
        Tracing.AddMetadata("metadata", "value");
        
        Tracing.WithSubsegment("localNamespace", "GetSomething", (subsegment) => {
            GetSomething();
        });
        
        await Task.FromResult(0);
    }

    private void GetSomething()
    {
        Tracing.AddAnnotation("getsomething", "value");
    }
}

public class FullExampleHandler2
{
    [Tracing]
    public static async Task<string> FunctionHandler(string request, ILambdaContext context)
    {
        string myIp = await GetIp();

        var response = CallDynamo(myIp);

        return "hello";
    }

    [Tracing(SegmentName = "Call DynamoDB")]
    private static List<string> CallDynamo(string myIp)
    {
        var newList = ToUpper(myIp, new List<string>() { "Hello", "World" });
        return newList;
    }

    [Tracing(SegmentName = "To Upper")]
    private static List<string> ToUpper(string myIp, List<string> response)
    {
        var newList = new List<string>();
        foreach (var item in response)
        {
            newList.Add(item.ToUpper());
        }

        newList.Add(myIp);
        return newList;
    }

    [Tracing(SegmentName = "Get Ip Address")]
    private static async Task<string> GetIp()
    {
        return await Task.FromResult("127.0.0.1");
    }
}

public class FullExampleHandler3
{
    [Tracing]
    public static async Task<string> FunctionHandler(string request, ILambdaContext context)
    {
        string myIp = GetIp();

        var response = CallDynamo(myIp);

        return "hello";
    }

    [Tracing(SegmentName = "Call DynamoDB")]
    private static List<string> CallDynamo(string myIp)
    {
        var newList = ToUpper(myIp, new List<string>() { "Hello", "World" });
        return newList;
    }

    [Tracing(SegmentName = "To Upper")]
    private static List<string> ToUpper(string myIp, List<string> response)
    {
        var newList = new List<string>();
        foreach (var item in response)
        {
            newList.Add(item.ToUpper());
        }

        newList.Add(myIp);
        return newList;
    }

    [Tracing(SegmentName = "Get Ip Address")]
    private static string GetIp()
    {
        return "127.0.0.1";
    }
}