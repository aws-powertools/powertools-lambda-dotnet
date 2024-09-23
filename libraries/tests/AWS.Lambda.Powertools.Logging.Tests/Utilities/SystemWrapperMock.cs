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

using System.IO;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Logging.Tests.Utilities;

public class SystemWrapperMock : ISystemWrapper
{
    private readonly IPowertoolsEnvironment _powertoolsEnvironment;
    public bool LogMethodCalled { get; private set; }
    public string LogMethodCalledWithArgument { get; private set; }

    public SystemWrapperMock(IPowertoolsEnvironment powertoolsEnvironment)
    {
        _powertoolsEnvironment = powertoolsEnvironment;
    }

    public string GetEnvironmentVariable(string variable)
    {
        return _powertoolsEnvironment.GetEnvironmentVariable(variable);
    }

    public void Log(string value)
    {
        LogMethodCalledWithArgument = value;
        LogMethodCalled = true;
    }
    
    public void LogLine(string value)
    {
        LogMethodCalledWithArgument = value;
        LogMethodCalled = true;
    }


    public double GetRandom()
    {
        return 0.7;
    }

    public void SetEnvironmentVariable(string variable, string value)
    {
        throw new System.NotImplementedException();
    }

    public void SetExecutionEnvironment<T>(T type)
    {
    }

    public void SetOut(TextWriter writeTo)
    {
        
    }
}