using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using LateToTheParty.Controllers;
using UnityEngine;
using UnityEngine.AI;

namespace LateToTheParty.Models
{
    public class DoorObstacle
    {
        public bool IsToggleable { get; private set; } = true;
        public Door LinkedDoor { get; private set; }

        private Collider collider;
        private NavMeshObstacle navMeshObstacle = null;
        private PathVisualizationData visualizationData = null;

        public Vector3? Position
        {
            get { return navMeshObstacle?.transform.position; }
        }

        public DoorObstacle(Collider _collider, Door _door)
        {
            collider = _collider;
            LinkedDoor = _door;
        }

        public DoorObstacle(Collider _collider, Door _door, bool istoggleable) : this(_collider, _door)
        {
            IsToggleable = istoggleable;
        }

        public void Update()
        {
            // Ensure the door isn't locked and nothing is interacting with it
            bool canOpenDoor = LinkedDoor.DoorState == EDoorState.Open;
            canOpenDoor |= LinkedDoor.DoorState == EDoorState.Shut;

            // Only remove the obstacle if the main player can toggle the door
            if (canOpenDoor && IsToggleable)
            {
                Remove();
                return;
            }

            Add();
        }

        public void Remove()
        {
            if (navMeshObstacle != null)
            {
                UnityEngine.Object.Destroy(navMeshObstacle);
                navMeshObstacle = null;
            }

            if (visualizationData != null)
            {
                PathRender.RemovePath(visualizationData);
                visualizationData.Clear();
                visualizationData = null;
            }
        }

        private void Add()
        {
            if (navMeshObstacle != null)
            {
                return;
            }

            string id = "Door_" + LinkedDoor.Id.Replace(" ", "_") + "_Obstacle";

            GameObject doorBlockerObj = new GameObject(id);
            doorBlockerObj.transform.SetParent(collider.transform);
            doorBlockerObj.transform.position = collider.bounds.center;

            navMeshObstacle = doorBlockerObj.AddComponent<NavMeshObstacle>();
            navMeshObstacle.size = collider.bounds.size;
            navMeshObstacle.carving = true;
            navMeshObstacle.carveOnlyStationary = false;

            if (!ConfigController.Config.Debug.LootPathVisualization.Enabled || !ConfigController.Config.Debug.LootPathVisualization.ShowDoorObstacles)
            {
                return;
            }

            // Draw ellipsoid around obstacle
            Vector3 ellipsoidSize = PathRender.IncreaseVector3ToMinSize(navMeshObstacle.size, ConfigController.Config.Debug.LootPathVisualization.DoorObstacleMinRadius);
            Vector3[] obstaclePoints = PathRender.GetEllipsoidPoints(LinkedDoor.transform.position, ellipsoidSize, ConfigController.Config.Debug.LootPathVisualization.PointsPerCircle);
            visualizationData = new PathVisualizationData(id, obstaclePoints, Color.yellow);
            PathRender.AddOrUpdatePath(visualizationData);
        }
    }
}
