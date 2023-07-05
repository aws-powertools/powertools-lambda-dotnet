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


namespace HelloWorld;

/// <summary>
/// Record to represent the data structure of Lookup
/// </summary>
[Serializable]
public class LookupRecord
{
    public LookupRecord()
    {
    }
    
    /// <summary>
    /// Record to represent the data structure of Lookup
    /// </summary>
    /// <param name="lookupId">Id of the lookup</param>
    /// <param name="greeting">Greeting phrase</param>
    /// <param name="ipAddress">IP address</param>
    public LookupRecord(string? lookupId, string? greeting, string? ipAddress)
    {
        this.LookupId = lookupId;
        this.Greeting = greeting;
        this.IpAddress = ipAddress;
    }

    public string? LookupId { get; set; }
    public string? Greeting { get; set; }
    public string? IpAddress { get; set; }
}