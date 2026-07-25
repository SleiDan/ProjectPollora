using UnityEngine;

public class PrototypeLevelBuilder : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerRespawnPoint;
    [SerializeField] private Transform polloraStartPoint;
    [SerializeField] private Transform polloraLeavePoint;
    [SerializeField] private Transform polloraInspectPoint;
    [SerializeField] private PolloraController polloraController;
    [SerializeField] private InteractableHidingSpot[] hidingSpots;

    [Header("Legacy Test Geometry")]
    [SerializeField] private Renderer legacyFloorRenderer;
    [SerializeField] private Collider legacyFloorCollider;
    [SerializeField] private GameObject[] legacyObstacles;

    private Material floorMaterial;
    private Material wallMaterial;
    private Material coverMaterial;
    private Material goalMaterial;

    private void Awake()
    {
        DisableLegacyGeometry();

        Transform bakedLevel = transform.Find("Three Room Prototype Level");

        if (bakedLevel != null)
        {
            AssignBakedPatrolPoints(bakedLevel);
            PlaceGameplayObjects();
            return;
        }

        CreateMaterials();
        BuildThreeRoomLayout();
        PlaceGameplayObjects();
    }

    private void AssignBakedPatrolPoints(Transform levelRoot)
    {
        if (polloraController == null)
            return;

        Transform patrolRoot = levelRoot.Find("02 Pollora Room/Patrol Points");

        if (patrolRoot == null)
        {
            Debug.LogError("The baked level is missing its Pollora patrol points.", this);
            return;
        }

        Transform[] points = new Transform[patrolRoot.childCount];

        for (int i = 0; i < patrolRoot.childCount; i++)
            points[i] = patrolRoot.GetChild(i);

        polloraController.SetPatrolPoints(points);
    }

    private void DisableLegacyGeometry()
    {
        if (legacyFloorRenderer != null)
            legacyFloorRenderer.enabled = false;

        if (legacyFloorCollider != null)
            legacyFloorCollider.enabled = false;

        if (legacyObstacles == null)
            return;

        for (int i = 0; i < legacyObstacles.Length; i++)
        {
            if (legacyObstacles[i] != null)
                legacyObstacles[i].SetActive(false);
        }
    }

    private void CreateMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("PrototypeLevelBuilder could not find a compatible material shader.", this);
            return;
        }

        floorMaterial = CreateMaterial(shader, "Prototype Floor", new Color(0.12f, 0.14f, 0.16f));
        wallMaterial = CreateMaterial(shader, "Prototype Walls", new Color(0.28f, 0.30f, 0.34f));
        coverMaterial = CreateMaterial(shader, "Main Room Cover", new Color(0.18f, 0.22f, 0.26f));
        goalMaterial = CreateMaterial(shader, "Goal Marker", new Color(0.1f, 0.75f, 0.3f));
    }

    private Material CreateMaterial(Shader shader, string materialName, Color color)
    {
        Material material = new Material(shader)
        {
            name = materialName,
            color = color
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        return material;
    }

    private void BuildThreeRoomLayout()
    {
        Transform levelRoot = new GameObject("Three Room Prototype Level").transform;
        levelRoot.SetParent(transform, false);

        Transform startRoom = CreateSection(levelRoot, "01 Start Room");
        Transform mainRoom = CreateSection(levelRoot, "02 Pollora Room");
        Transform goalRoom = CreateSection(levelRoot, "03 Goal Room");

        CreateBlock(startRoom, "Start Floor", new Vector3(0f, -0.25f, -20f), new Vector3(10f, 0.5f, 10f), floorMaterial);
        CreateBlock(mainRoom, "Main Floor", new Vector3(0f, -0.25f, 0f), new Vector3(24f, 0.5f, 24f), floorMaterial);
        CreateBlock(goalRoom, "Goal Floor", new Vector3(0f, -0.25f, 20f), new Vector3(10f, 0.5f, 10f), floorMaterial);
        CreateBlock(levelRoot, "South Connector Floor", new Vector3(0f, -0.25f, -13.5f), new Vector3(3f, 0.5f, 3f), floorMaterial);
        CreateBlock(levelRoot, "North Connector Floor", new Vector3(0f, -0.25f, 13.5f), new Vector3(3f, 0.5f, 3f), floorMaterial);

        BuildStartRoomWalls(startRoom);
        BuildMainRoomWalls(mainRoom);
        BuildGoalRoomWalls(goalRoom);
        BuildConnectors(levelRoot);
        BuildMainRoomCover(mainRoom);
        CreatePatrolPoints(mainRoom);

        GameObject goalMarker = CreateBlock(
            goalRoom,
            "Goal Marker",
            new Vector3(0f, 0.03f, 21.5f),
            new Vector3(4f, 0.06f, 4f),
            goalMaterial
        );
        Collider goalCollider = goalMarker.GetComponent<Collider>();

        if (goalCollider != null)
            goalCollider.enabled = false;
    }

    private Transform CreateSection(Transform parent, string sectionName)
    {
        Transform section = new GameObject(sectionName).transform;
        section.SetParent(parent, false);
        return section;
    }

    private void BuildStartRoomWalls(Transform root)
    {
        CreateBlock(root, "West Wall", new Vector3(-5.25f, 2f, -20f), new Vector3(0.5f, 4f, 10.5f), wallMaterial);
        CreateBlock(root, "East Wall", new Vector3(5.25f, 2f, -20f), new Vector3(0.5f, 4f, 10.5f), wallMaterial);
        CreateBlock(root, "South Wall", new Vector3(0f, 2f, -25.25f), new Vector3(10.5f, 4f, 0.5f), wallMaterial);
        CreateDoorwayWall(root, "North", -14.75f, 10.5f);
    }

    private void BuildMainRoomWalls(Transform root)
    {
        CreateBlock(root, "West Wall", new Vector3(-12.25f, 2f, 0f), new Vector3(0.5f, 4f, 24.5f), wallMaterial);
        CreateBlock(root, "East Wall", new Vector3(12.25f, 2f, 0f), new Vector3(0.5f, 4f, 24.5f), wallMaterial);
        CreateDoorwayWall(root, "South", -12.25f, 24.5f);
        CreateDoorwayWall(root, "North", 12.25f, 24.5f);
    }

    private void BuildGoalRoomWalls(Transform root)
    {
        CreateBlock(root, "West Wall", new Vector3(-5.25f, 2f, 20f), new Vector3(0.5f, 4f, 10.5f), wallMaterial);
        CreateBlock(root, "East Wall", new Vector3(5.25f, 2f, 20f), new Vector3(0.5f, 4f, 10.5f), wallMaterial);
        CreateDoorwayWall(root, "South", 14.75f, 10.5f);
        CreateBlock(root, "North Wall", new Vector3(0f, 2f, 25.25f), new Vector3(10.5f, 4f, 0.5f), wallMaterial);
    }

    private void CreateDoorwayWall(Transform root, string sideName, float z, float totalWidth)
    {
        const float doorwayWidth = 3f;
        float segmentWidth = (totalWidth - doorwayWidth) * 0.5f;
        float segmentOffset = doorwayWidth * 0.5f + segmentWidth * 0.5f;

        CreateBlock(root, sideName + " Wall Left", new Vector3(-segmentOffset, 2f, z), new Vector3(segmentWidth, 4f, 0.5f), wallMaterial);
        CreateBlock(root, sideName + " Wall Right", new Vector3(segmentOffset, 2f, z), new Vector3(segmentWidth, 4f, 0.5f), wallMaterial);
    }

    private void BuildConnectors(Transform root)
    {
        CreateBlock(root, "South Connector West Wall", new Vector3(-1.75f, 2f, -13.5f), new Vector3(0.5f, 4f, 3f), wallMaterial);
        CreateBlock(root, "South Connector East Wall", new Vector3(1.75f, 2f, -13.5f), new Vector3(0.5f, 4f, 3f), wallMaterial);
        CreateBlock(root, "North Connector West Wall", new Vector3(-1.75f, 2f, 13.5f), new Vector3(0.5f, 4f, 3f), wallMaterial);
        CreateBlock(root, "North Connector East Wall", new Vector3(1.75f, 2f, 13.5f), new Vector3(0.5f, 4f, 3f), wallMaterial);
    }

    private void BuildMainRoomCover(Transform root)
    {
        CreateBlock(root, "Cover A", new Vector3(-4f, 1.5f, -6f), new Vector3(0.75f, 3f, 8f), coverMaterial);
        CreateBlock(root, "Cover B", new Vector3(4f, 1.5f, 0f), new Vector3(0.75f, 3f, 10f), coverMaterial);
        CreateBlock(root, "Cover C", new Vector3(-4f, 1.5f, 6f), new Vector3(0.75f, 3f, 8f), coverMaterial);
        CreateBlock(root, "Cover D", new Vector3(8f, 1.5f, 7f), new Vector3(5f, 3f, 0.75f), coverMaterial);
    }

    private void CreatePatrolPoints(Transform root)
    {
        Transform patrolRoot = CreateSection(root, "Patrol Points");
        Vector3[] positions =
        {
            new Vector3(0f, 0f, -10f),
            new Vector3(-9f, 0f, -9f),
            new Vector3(9f, 0f, -9f),
            new Vector3(-9f, 0f, 0f),
            new Vector3(9f, 0f, 0f),
            new Vector3(-9f, 0f, 10f),
            new Vector3(4f, 0f, 10f),
            new Vector3(0f, 0f, 11f)
        };
        Transform[] patrolPoints = new Transform[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            Transform patrolPoint = new GameObject($"Patrol Point {i + 1}").transform;
            patrolPoint.SetParent(patrolRoot, false);
            patrolPoint.position = positions[i];
            patrolPoints[i] = patrolPoint;
        }

        if (polloraController != null)
            polloraController.SetPatrolPoints(patrolPoints);
    }

    private GameObject CreateBlock(Transform parent, string blockName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = blockName;
        block.transform.SetParent(parent, false);
        block.transform.position = position;
        block.transform.localScale = scale;

        Renderer blockRenderer = block.GetComponent<Renderer>();

        if (blockRenderer != null && material != null)
            blockRenderer.sharedMaterial = material;

        return block;
    }

    private void PlaceGameplayObjects()
    {
        MoveCharacter(player, new Vector3(0f, 1f, -21f), Quaternion.identity);
        SetTransform(playerRespawnPoint, new Vector3(0f, 1f, -21f), Quaternion.identity);
        SetTransform(polloraStartPoint, new Vector3(0f, 1f, 0f), Quaternion.Euler(0f, 180f, 0f));
        SetTransform(polloraInspectPoint, new Vector3(0f, 1f, -5f), Quaternion.Euler(0f, 180f, 0f));
        SetTransform(polloraLeavePoint, new Vector3(10f, 1f, 10f), Quaternion.Euler(0f, 180f, 0f));

        PlaceHidingSpot(0, new Vector3(-10.5f, 1f, -7f), Quaternion.Euler(0f, -90f, 0f));
        PlaceHidingSpot(1, new Vector3(10.5f, 1f, 1f), Quaternion.Euler(0f, 90f, 0f));
        PlaceHidingSpot(2, new Vector3(-10.5f, 1f, 8f), Quaternion.Euler(0f, -90f, 0f));
    }

    private void MoveCharacter(Transform target, Vector3 position, Quaternion rotation)
    {
        if (target == null)
            return;

        CharacterController characterController = target.GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        target.SetPositionAndRotation(position, rotation);

        if (characterController != null)
            characterController.enabled = true;
    }

    private void SetTransform(Transform target, Vector3 position, Quaternion rotation)
    {
        if (target != null)
            target.SetPositionAndRotation(position, rotation);
    }

    private void PlaceHidingSpot(int index, Vector3 position, Quaternion rotation)
    {
        if (hidingSpots == null ||
            index < 0 ||
            index >= hidingSpots.Length ||
            hidingSpots[index] == null)
        {
            return;
        }

        hidingSpots[index].transform.SetPositionAndRotation(position, rotation);
    }

    private void OnDestroy()
    {
        Destroy(floorMaterial);
        Destroy(wallMaterial);
        Destroy(coverMaterial);
        Destroy(goalMaterial);
    }
}
