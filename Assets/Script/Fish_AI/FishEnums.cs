// Scripts/Fish/Core/FishEnums.cs

public enum FishEnvironment
{
    None,
    Ocean,
    Aquarium,
    Captured
}

public enum FishStateType
{
    None,
    OceanPatrol,
    AquariumIdle,
    Flee,
    Feeding,
    Hiding,
    Schooling,
    Sick,
    Dead
}

// ✏️ DITAMBAH: Enum untuk arah forward ikan
public enum ForwardDirection
{
    Z_Positive,   // Default Unity (0, 0, 1)
    Z_Negative,   // (0, 0, -1)
    X_Positive,   // (1, 0, 0)
    X_Negative,   // (-1, 0, 0)
    Y_Positive,   // (0, 1, 0)
    Y_Negative    // (0, -1, 0)
}