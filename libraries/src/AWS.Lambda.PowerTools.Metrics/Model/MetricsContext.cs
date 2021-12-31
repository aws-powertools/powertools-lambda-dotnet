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

using System;
using System.Collections.Generic;
using System.Linq;

namespace AWS.Lambda.PowerTools.Metrics
{
    public class MetricsContext : IDisposable
    {
        private RootNode _rootNode;
        internal bool IsSerializable => !(GetMetrics().Count == 0 && _rootNode.AWS.CustomMetadata.Count == 0);

        /// <summary>
        /// Creates empty MetricsContext object
        /// </summary>
        public MetricsContext()
        {
            _rootNode = new RootNode();
        }

        /// <summary>
        /// Retrieves all metrics stored in memory
        /// </summary>
        /// <returns>List of Metrics</returns>
        public List<MetricDefinition> GetMetrics()
        {
            return _rootNode.AWS.GetMetrics();
        }

        /// <summary>
        /// Clears all metrics from memory
        /// </summary>
        public void ClearMetrics()
        {
            _rootNode.AWS.ClearMetrics();
        }

        /// <summary>
        /// Clears non-default dimensions from memory
        /// </summary>
        internal void ClearNonDefaultDimensions()
        {
            _rootNode.AWS.ClearNonDefaultDimensions();
        }

        /// <summary>
        /// Adds Metric to memory
        /// </summary>
        /// <param name="key">Metric key. Cannot be null, empty or whitespace</param>
        /// <param name="value">Metric value</param>
        /// <param name="unit">Metric unit</param>
        public void AddMetric(string key, double value, MetricUnit unit)
        {
            _rootNode.AWS.AddMetric(key, value, unit);
        }

        /// <summary>
        /// Sets metrics namespace identifier
        /// </summary>
        /// <param name="metricNamespace">Metrics namespace identifier</param>
        public void SetNamespace(string metricNamespace)
        {
            _rootNode.AWS.SetNamespace(metricNamespace);
        }

        /// <summary>
        /// Retrieves metrics namespace identifier from memory
        /// </summary>
        /// <returns>Metrics namespace identifier</returns>
        internal string GetNamespace()
        {
            return _rootNode.AWS.GetNamespace();
        }

        /// <summary>
        /// Sets service name identifier
        /// </summary>
        /// <param name="service">Service name</param>
        public void SetService(string service)
        {
            _rootNode.AWS.SetService(service);
        }

        /// <summary>
        /// Retrieves service name from memory
        /// </summary>
        /// <returns>Service name</returns>
        internal string GetService()
        {
            return _rootNode.AWS.GetService();
        }

        /// <summary>
        /// Adds new dimension to memory
        /// </summary>
        /// <param name="key">Dimension key. Cannot be null, empty or whitespace</param>
        /// <param name="value">Dimension value</param>
        public void AddDimension(string key, string value)
        {
            _rootNode.AWS.AddDimensionSet(new DimensionSet(key, value));
        }

        /// <summary>
        /// Sets default dimensions list
        /// </summary>
        /// <param name="defaultDimensions">Default dimensions list</param>
        public void SetDefaultDimensions(List<DimensionSet> defaultDimensions){
            _rootNode.AWS.SetDefaultDimensions(defaultDimensions);
        }

        /// <summary>
        /// Adds metadata to memory
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        public void AddMetadata(string key, dynamic value)
        {
            _rootNode.AWS.AddMetadata(key, value);
        }

        /// <summary>
        /// Serializes metrics object to string using Embedded Metric Format (EMF)
        /// </summary>
        /// <returns>String object representing all metrics in memory</returns>
        public string Serialize()
        {
            return _rootNode.Serialize();
        }

        /// <summary>
        /// Implements IDisposable interface
        /// </summary>
        public void Dispose()
        {
            _rootNode = null;
        }
    }
}