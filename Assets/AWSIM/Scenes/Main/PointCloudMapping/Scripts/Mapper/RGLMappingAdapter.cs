// Copyright 2022 Robotec.ai.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using RGLUnityPlugin;
using UnityEngine;

namespace AWSIM.PointCloudMapping
{
    /// <summary>
    /// Implementation of an adapter to LidarSensor based on RGL for PointCloudMapper.
    /// </summary>
    [RequireComponent(typeof(LidarSensor))]
    public class RGLMappingAdapter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Resolution to sub-sample point cloud data. Set leaf size to 0 if you don't want to sub-sample.")]
        private float leafSize;

        private Vector3 worldOriginROS;
        private string outputPCDFilePath = "output.pcd";

        public PointCloudMapper mapper;

        private LidarSensor lidarSensor;

        private RGLNodeSequence rglSubgraphMapping;

        private readonly string rosWorldTransformNodeId = "ROS_WORLD_TF";
        private readonly string downsampleNodeId = "DOWNSAMPLE";
        private readonly string writePcdNodeId = "WRITE_PCD";

        public void Start()
        {
            lidarSensor = GetComponent<LidarSensor>();
            // Make sure automatic capture in RGL Lidar Sensor is disabled.
            // We want to perform captures only on demand (after warping).
            lidarSensor.AutomaticCaptureHz = 0;
        }

        public string GetSensorName()
        {
            return gameObject.name;
        }

        public void SavePcd()
        {
            rglSubgraphMapping.Clear();
        }

        public void Capture()
        {
            if (rglSubgraphMapping == null)
            {
                if (mapper == null)
                {
                    throw new Exception("Attempted to run RGLMappingAdapter without mapper attached!");
                }
                worldOriginROS = mapper.GetWorldOriginROS();
                outputPCDFilePath = mapper.GetOutputPcdFilePath();

                // Create and connect subgraph
                Matrix4x4 worldTransform = ROS2.Transformations.Unity2RosMatrix4x4();
                worldTransform.SetColumn(3, worldTransform.GetColumn(3) + (Vector4) worldOriginROS);
                rglSubgraphMapping = new RGLNodeSequence()
                    .AddNodePointsTransform(rosWorldTransformNodeId, worldTransform);

                if (leafSize > 0.0f)
                {
                    rglSubgraphMapping.AddNodePointsDownsample(downsampleNodeId, new Vector3(leafSize, leafSize, leafSize));
                }

                rglSubgraphMapping.AddNodePointsWritePCDFile(writePcdNodeId, outputPCDFilePath);

                lidarSensor.ConnectToWorldFrame(rglSubgraphMapping);
            }

            lidarSensor.Capture();
        }
    }
}
