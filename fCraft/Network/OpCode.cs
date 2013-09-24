// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> Minecraft protocol's opcodes. 
    /// For detailed explanation of Minecraft Classic protocol, see http://wiki.vg/Classic_Protocol </summary>
    public enum OpCode {
        Handshake = 0,
        Ping = 1,
        MapBegin = 2,
        MapChunk = 3,
        MapEnd = 4,
        SetBlockClient = 5,
        SetBlockServer = 6,
        AddEntity = 7,
        Teleport = 8,
        MoveRotate = 9,
        Move = 10,
        Rotate = 11,
        RemoveEntity = 12,
        Message = 13,
        Kick = 14,
        SetPermission = 15,

        // CPE
        ExtInfo = 16,
        ExtEntry = 17,
        ClickDistance = 18,
        CustomBlockSupportLevel = 19,
        HeldBlock = 20,
        TextHotKey = 21,
        ExtAddPlayerName = 22,
        ExtAddEntity = 23,
        ExtRemovePlayerName = 24,
        EnvSetColor = 25,
        SelectionCuboid = 26,
        RemoveSelectionCuboid = 27,
        SetBlockPermission = 28,
        ChangeModel = 29,
        EnvSetMapAppearance = 30
    }
}