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

        CreateBlock(startRoom, "Start Floor", new Vector3(-12f, -0.25f, -27f), new Vector3(10f, 0.5f, 10f), floorMaterial);
        CreateBlock(mainRoom, "Main Floor", new Vector3(0f, -0.25f, 0f), new Vector3(36f, 0.5f, 40f), floorMaterial);
        CreateBlock(goalRoom, "Goal Floor", new Vector3(12f, -0.25f, 27f), new Vector3(10f, 0.5f, 10f), floorMaterial);
        CreateBlock(levelRoot, "South Connector Floor", new Vector3(-12f, -0.25f, -21.5f), new Vector3(3f, 0.5f, 3f), floorMaterial);
        CreateBlock(levelRoot, "North Connector Floor", new Vector3(12f, -0.25f, 21.5f), new Vector3(3f, 0.5f, 3f), floorMaterial);

        BuildStartRoomWalls(startRoom);
        BuildMainRoomWalls(mainRoom);
        BuildGoalRoomWalls(goalRoom);
        BuildConnectors(levelRoot);
        BuildMainRoomCover(mainRoom);
        CreatePatrolPoints(mainRoom);

        GameObject goalMarker = CreateBlock(
            goalRoom,
            "Goal Marker",
            new Vector3(12f, 0.03f, 28.5f),
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
        CreateBlock(root, "West Wall", new Vector3(-17.25f, 2f, -27f), new Vector3(0.5f, 4f, 10.5f), wallMaterial);
        CreateBlock(root, "East Wall", new Vector3(-6.75f, 2f, -27f), new Vector3(0.5f, 4f, 10.5f), wallMaterial);
        CreateBlock(root, "South Wall", new Vector3(-12f, 2f, -32.25f), new Vector3(10.5f, 4f, 0.5f), wallMaterial);
        CreateOffsetDoorwayWall(root, "North", -21.75f, -12f, -12f, 10.5f);
    }

    private void BuildMainRoomWalls(Transform root)
    {
        CreateBlock(root, "West Wall", new Vector3(-18.25f, 2f, 0f), new Vector3(0.5f, 4f, 40.5f), wallMaterial);
        CreateBlock(root, "East Wall", new Vector3(18.25f, 2f, 0f), new Vector3(0.5f, 4f, 40.5f), wallMaterial);
        CreateOffsetDoorwayWall(root, "South", -20.25f, 0f, -12f, 36.5f);
        CreateOffsetDoorwayWall(root, "North", 20.25f, 0f, 12f, 36.5f);
    }

    private void BuildGoalRoomWalls(Transform root)
    {
        CreateBlock(root, "West Wall", new Vector3(6.75f, 2f, 27f), new Vector3(0.5f, 4f, 10.5f), wallMaterial);
        CreateBlock(root, "East Wall", new Vector3(17.25f, 2f, 27f), new Vector3(0.5f, 4f, 10.5f), wallMaterial);
        CreateOffsetDoorwayWall(root, "South", 21.75f, 12f, 12f, 10.5f);
        CreateBlock(root, "North Wall", new Vector3(12f, 2f, 32.25f), new Vector3(10.5f, 4f, 0.5f), wallMaterial);
    }

    private void CreateDoorwayWall(Transform root, string sideName, float z, float totalWidth)
    {
        const float doorwayWidth = 3f;
        float segmentWidth = (totalWidth - doorwayWidth) * 0.5f;
        float segmentOffset = doorwayWidth * 0.5f + segmentWidth * 0.5f;

        CreateBlock(root, sideName + " Wall Left", new Vector3(-segmentOffset, 2f, z), new Vector3(segmentWidth, 4f, 0.5f), wallMaterial);
        CreateBlock(root, sideName + " Wall Right", new Vector3(segmentOffset, 2f, z), new Vector3(segmentWidth, 4f, 0.5f), wallMaterial);
    }

    private void CreateOffsetDoorwayWall(Transform root, string sideName, float z, float wallCenter, float doorwayCenter, float totalWidth)
    {
        const float doorwayWidth = 3f;
        float minX = wallCenter - totalWidth * 0.5f;
        float maxX = wallCenter + totalWidth * 0.5f;
        float leftWidth = doorwayCenter - doorwayWidth * 0.5f - minX;
        float rightWidth = maxX - doorwayCenter - doorwayWidth * 0.5f;

        CreateBlock(root, sideName + " Wall Left", new Vector3(minX + leftWidth * 0.5f, 2f, z), new Vector3(leftWidth, 4f, 0.5f), wallMaterial);
        CreateBlock(root, sideName + " Wall Right", new Vector3(maxX - rightWidth * 0.5f, 2f, z), new Vector3(rightWidth, 4f, 0.5f), wallMaterial);
    }

    private void BuildConnectors(Transform root)
    {
        CreateBlock(root, "South Connector West Wall", new Vector3(-13.75f, 2f, -21.5f), new Vector3(0.5f, 4f, 3f), wallMaterial);
        CreateBlock(root, "South Connector East Wall", new Vector3(-10.25f, 2f, -21.5f), new Vector3(0.5f, 4f, 3f), wallMaterial);
        CreateBlock(root, "North Connector West Wall", new Vector3(10.25f, 2f, 21.5f), new Vector3(0.5f, 4f, 3f), wallMaterial);
        CreateBlock(root, "North Connector East Wall", new Vector3(13.75f, 2f, 21.5f), new Vector3(0.5f, 4f, 3f), wallMaterial);
    }

    private void BuildMainRoomCover(Transform root)
    {
        Transform rooms = CreateSection(root, "Interior Rooms");

        CreateBlock(rooms, "Storage East Wall South", new Vector3(-7f, 2f, -15.5f), new Vector3(0.5f, 4f, 5f), wallMaterial);
        CreateBlock(rooms, "Storage East Wall North", new Vector3(-7f, 2f, -8f), new Vector3(0.5f, 4f, 4f), wallMaterial);
        CreateBlock(rooms, "Storage North Wall", new Vector3(-12.5f, 2f, -6f), new Vector3(11f, 4f, 0.5f), wallMaterial);

        CreateBlock(rooms, "Workshop West Wall South", new Vector3(5f, 2f, -15f), new Vector3(0.5f, 4f, 6f), wallMaterial);
        CreateBlock(rooms, "Workshop West Wall North", new Vector3(5f, 2f, -7f), new Vector3(0.5f, 4f, 4f), wallMaterial);
        CreateBlock(rooms, "Workshop North Wall Left", new Vector3(8f, 2f, -5f), new Vector3(6f, 4f, 0.5f), wallMaterial);
        CreateBlock(rooms, "Workshop North Wall Right", new Vector3(15.5f, 2f, -5f), new Vector3(5f, 4f, 0.5f), wallMaterial);

        CreateBlock(rooms, "Archive South Wall Left", new Vector3(-15f, 2f, 4f), new Vector3(6f, 4f, 0.5f), wallMaterial);
        CreateBlock(rooms, "Archive South Wall Right", new Vector3(-8f, 2f, 4f), new Vector3(4f, 4f, 0.5f), wallMaterial);
        CreateBlock(rooms, "Archive East Wall South", new Vector3(-6f, 2f, 6.5f), new Vector3(0.5f, 4f, 5f), wallMaterial);
        CreateBlock(rooms, "Archive East Wall North", new Vector3(-6f, 2f, 14.5f), new Vector3(0.5f, 4f, 5f), wallMaterial);
        CreateBlock(rooms, "Archive North Wall", new Vector3(-12f, 2f, 17f), new Vector3(12f, 4f, 0.5f), wallMaterial);

        CreateBlock(rooms, "Security West Wall South", new Vector3(5f, 2f, 5f), new Vector3(0.5f, 4f, 4f), wallMaterial);
        CreateBlock(rooms, "Security West Wall North", new Vector3(5f, 2f, 12.5f), new Vector3(0.5f, 4f, 5f), wallMaterial);
        CreateBlock(rooms, "Security South Wall Left", new Vector3(7.5f, 2f, 3f), new Vector3(5f, 4f, 0.5f), wallMaterial);
        CreateBlock(rooms, "Security South Wall Right", new Vector3(15.5f, 2f, 3f), new Vector3(5f, 4f, 0.5f), wallMaterial);
        CreateBlock(rooms, "Security North Wall Left", new Vector3(8f, 2f, 15f), new Vector3(6f, 4f, 0.5f), wallMaterial);
        CreateBlock(rooms, "Security North Wall Right", new Vector3(16f, 2f, 15f), new Vector3(4f, 4f, 0.5f), wallMaterial);

        Transform props = CreateSection(root, "Exploration Cover");
        CreateBlock(props, "Storage Shelves", new Vector3(-13f, 1.5f, -12f), new Vector3(1.2f, 3f, 5f), coverMaterial);
        CreateBlock(props, "Workshop Bench", new Vector3(12f, 1.25f, -10f), new Vector3(5f, 2.5f, 1.2f), coverMaterial);
        CreateBlock(props, "Central Kiosk", new Vector3(0f, 1.5f, 0f), new Vector3(4f, 3f, 4f), coverMaterial);
        CreateBlock(props, "Archive Shelves A", new Vector3(-13f, 1.5f, 8f), new Vector3(1f, 3f, 5f), coverMaterial);
        CreateBlock(props, "Archive Shelves B", new Vector3(-9f, 1.5f, 13f), new Vector3(5f, 3f, 1f), coverMaterial);
        CreateBlock(props, "Security Desk", new Vector3(11f, 1.25f, 9f), new Vector3(5f, 2.5f, 1.2f), coverMaterial);
    }

    private void CreatePatrolPoints(Transform root)
    {
        Transform patrolRoot = CreateSection(root, "Patrol Points");
        Vector3[] positions =
        {
            new Vector3(-12f, 0f, -17f),
            new Vector3(-12f, 0f, -8f),
            new Vector3(0f, 0f, -12f),
            new Vector3(11f, 0f, -16f),
            new Vector3(10f, 0f, -2f),
            new Vector3(0f, 0f, 7f),
            new Vector3(-13f, 0f, 15f),
            new Vector3(-3f, 0f, 16f),
            new Vector3(10f, 0f, 17f),
            new Vector3(16f, 0f, 8f)
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
        MoveCharacter(player, new Vector3(-12f, 1f, -28f), Quaternion.identity);
        SetTransform(playerRespawnPoint, new Vector3(-12f, 1f, -28f), Quaternion.identity);
        SetTransform(polloraStartPoint, new Vector3(1f, 1f, 9f), Quaternion.Euler(0f, 180f, 0f));
        SetTransform(polloraInspectPoint, new Vector3(-12f, 1f, -9f), Quaternion.Euler(0f, 180f, 0f));
        SetTransform(polloraLeavePoint, new Vector3(16f, 1f, 18f), Quaternion.Euler(0f, 180f, 0f));

        PlaceHidingSpot(0, new Vector3(-16.5f, 1f, -10f), Quaternion.Euler(0f, -90f, 0f));
        PlaceHidingSpot(1, new Vector3(16.5f, 1f, -9f), Quaternion.Euler(0f, 90f, 0f));
        PlaceHidingSpot(2, new Vector3(-16.5f, 1f, 11f), Quaternion.Euler(0f, -90f, 0f));
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
