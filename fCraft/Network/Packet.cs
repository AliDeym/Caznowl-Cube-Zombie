// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Packet struct, just a wrapper for a byte array. </summary>
    public struct Packet {
        /// <summary> ID byte used in the protocol to indicate that an action should apply to self.
        /// When used in AddEntity packet, sets player's own respawn point.
        /// When used in Teleport packet, teleports the player. </summary>
        public const sbyte SelfID = -1;

        /// <summary> Raw bytes of this packet. </summary>
        public readonly byte[] Bytes;

        /// <summary> OpCode (first byte) of this packet. </summary>
        public OpCode OpCode {
            get { return (OpCode)Bytes[0]; }
        }


        /// <summary> Creates a new packet from given raw bytes. Data not be null. </summary>
        public Packet( [NotNull] byte[] rawBytes ) {
            if( rawBytes == null ) throw new ArgumentNullException( "rawBytes" );
            Bytes = rawBytes;
        }


        /// <summary> Creates a packet of correct size for a given opCode,
        /// and sets the first (opCode) byte. </summary>
        public Packet( OpCode opCode ) {
            Bytes = new byte[PacketSizes[(int)opCode]];
            Bytes[0] = (byte)opCode;
        }


        #region Packet Making

        public static Packet MakeHandshake( [NotNull] Player player, [NotNull] string serverName, [NotNull] string motd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( serverName == null ) throw new ArgumentNullException( "serverName" );
            if( motd == null ) throw new ArgumentNullException( "motd" );

            Packet packet = new Packet( OpCode.Handshake );
            packet.Bytes[1] = Config.ProtocolVersion;
            Encoding.ASCII.GetBytes( serverName.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            Encoding.ASCII.GetBytes( motd.PadRight( 64 ), 0, 64, packet.Bytes, 66 );
            packet.Bytes[130] = (byte)( player.Can( Permission.DeleteAdmincrete ) ? 100 : 0 );
            return packet;
        }


        public static Packet MakeSetBlock( short x, short y, short z, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( x, packet.Bytes, 1 );
            ToNetOrder( z, packet.Bytes, 3 );
            ToNetOrder( y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        public static Packet MakeSetBlock( Vector3I coords, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( (short)coords.X, packet.Bytes, 1 );
            ToNetOrder( (short)coords.Z, packet.Bytes, 3 );
            ToNetOrder( (short)coords.Y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        public static Packet MakeAddEntity( sbyte id, [NotNull] string name, Position pos ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            Packet packet = new Packet( OpCode.AddEntity );
            packet.Bytes[1] = (byte)id;
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            ToNetOrder( pos.X, packet.Bytes, 66 );
            ToNetOrder( pos.Z, packet.Bytes, 68 );
            ToNetOrder( pos.Y, packet.Bytes, 70 );
            packet.Bytes[72] = pos.R;
            packet.Bytes[73] = pos.L;
            return packet;
        }


        public static Packet MakeExtInfo(short extCount)
        {
            // Logger.Log( "Send: ExtInfo({0},{1})", Server.VersionString, extCount );
            Packet packet = new Packet(OpCode.ExtInfo);
            Encoding.ASCII.GetBytes(Updater.CurrentRelease.VersionString.PadRight(64), 0, 64, packet.Bytes, 1);
            ToNetOrder(extCount, packet.Bytes, 65);
            return packet;
        }

        public static Packet MakeExtEntry(string name, int version)
        {
            // Logger.Log( "Send: ExtEntry({0},{1})", name, version );
            Packet packet = new Packet(OpCode.ExtEntry);
            Encoding.ASCII.GetBytes(name.PadRight(64), 0, 64, packet.Bytes, 1);
            ToNetOrder(version, packet.Bytes, 65);
            return packet;
        }

        public static Packet MakeCustomBlockSupportLevel(byte level)
        {
            // Logger.Log( "Send: CustomBlockSupportLevel({0})", level );
            Packet packet = new Packet(OpCode.CustomBlockSupportLevel);
            packet.Bytes[1] = level;
            return packet;
        }

        public static Packet MakeSetBlockPermission(Block block, bool canPlace, bool canDelete)
        {
            Packet packet = new Packet(OpCode.SetBlockPermission);
            packet.Bytes[1] = (byte)block;
            packet.Bytes[2] = (byte)(canPlace ? 1 : 0);
            packet.Bytes[3] = (byte)(canDelete ? 1 : 0);
            return packet;
        }

        #region Mapping CPE Level 1
        public static Packet ClickDistance(short count)
        {
            Packet packet = new Packet(OpCode.ClickDistance);
            ToNetOrder(count, packet.Bytes, 1);
            return packet;
        }

        public static Packet HeldBlock(Block block, bool allow)
        {
            byte getb = 0;
            if (allow)
            {
                getb = 1;
            }
            Packet packet = new Packet(OpCode.HeldBlock);
            packet.Bytes[1] = (byte)block;
            packet.Bytes[2] = getb;
            return packet;
        }

        public static Packet SetTextHotKey(string read, string action, int Key, byte Mode)
        {
            Packet packet = new Packet(OpCode.TextHotKey);
            Encoding.ASCII.GetBytes(read.PadRight(64), 0, 64, packet.Bytes, 1);
            Encoding.ASCII.GetBytes(action.PadRight(64), 0, 64, packet.Bytes, 65);
            ToNetOrder(Key, packet.Bytes, 129);
            packet.Bytes[133] = Mode;
            return packet;
        }

        public static Packet ExtAddPlayerName(short nameid, string name, string ListName, string group, byte perm)
        {
            Packet packet = new Packet(OpCode.ExtAddPlayerName);
            ToNetOrder(nameid, packet.Bytes, 1);
            Encoding.ASCII.GetBytes(name.PadRight(64), 0, 64, packet.Bytes, 3);
            Encoding.ASCII.GetBytes(ListName.PadRight(64), 0, 64, packet.Bytes, 67);
            Encoding.ASCII.GetBytes(group.PadRight(64), 0, 64, packet.Bytes, 131);
            packet.Bytes[132] = perm;
            return packet;
        }

        public static Packet ExtAddEntity(byte id, string name, string nick)
        {
            Packet packet = new Packet(OpCode.ExtAddEntity);
            packet.Bytes[1] = id;
            Encoding.ASCII.GetBytes(name.PadRight(64), 0, 64, packet.Bytes, 2);
            Encoding.ASCII.GetBytes(nick.PadRight(64), 0, 64, packet.Bytes, 67);
            return packet;
        }

        public static Packet ExtRemovePlayerName(short nameid)
        {
            Packet packet = new Packet(OpCode.ExtRemovePlayerName);
            ToNetOrder(nameid, packet.Bytes, 1);
            return packet;
        }

        public static Packet EnvSetColor(byte mod, short r, short g, short b)
        {
            Packet packet = new Packet(OpCode.EnvSetColor);
            packet.Bytes[1] = mod;
            ToNetOrder(r, packet.Bytes, 2);
            ToNetOrder(g, packet.Bytes, 4);
            ToNetOrder(b, packet.Bytes, 6);
            return packet;
        }

        public static Packet MakeSelection(byte id, string label, short sx, short sy, short sz, short x, short y, short z, short r, short g, short b, short opa)
        {
            Packet packet = new Packet(OpCode.SelectionCuboid);
            packet.Bytes[1] = id;
            Encoding.ASCII.GetBytes(label.PadRight(64), 0, 64, packet.Bytes, 2);
            ToNetOrder(sx, packet.Bytes, 66);
            ToNetOrder(sy, packet.Bytes, 68);
            ToNetOrder(sz, packet.Bytes, 70);
            ToNetOrder(x, packet.Bytes, 72);
            ToNetOrder(y, packet.Bytes, 74);
            ToNetOrder(z, packet.Bytes, 76);
            ToNetOrder(r, packet.Bytes, 78);
            ToNetOrder(g, packet.Bytes, 80);
            ToNetOrder(b, packet.Bytes, 82);
            ToNetOrder(opa, packet.Bytes, 84);
            return packet;
        }

        public static Packet RemoveSelection(byte id)
        {
            Packet packet = new Packet(OpCode.RemoveSelectionCuboid);
            packet.Bytes[1] = id;
            return packet;
        }

        public static Packet ChangeModel(byte id, string model)
        {
            Packet packet = new Packet(OpCode.ChangeModel);
            packet.Bytes[1] = id;
            Encoding.ASCII.GetBytes(model.PadRight(64), 0, 64, packet.Bytes, 2);
            return packet;
        }

        public static Packet SetEnv(string url, Block side, Block edge, short level)
        {
            Packet packet = new Packet(OpCode.EnvSetMapAppearance);
            Encoding.ASCII.GetBytes(url.PadRight(64), 0, 64, packet.Bytes, 1);
            packet.Bytes[65] = (byte)side;
            packet.Bytes[66] = (byte)edge;
            ToNetOrder(level, packet.Bytes, 67);
            return packet;
        }
        #endregion

        public static Packet MakeTeleport( sbyte id, Position pos ) {
            Packet packet = new Packet( OpCode.Teleport );
            packet.Bytes[1] = (byte)id;
            ToNetOrder( pos.X, packet.Bytes, 2 );
            ToNetOrder( pos.Z, packet.Bytes, 4 );
            ToNetOrder( pos.Y, packet.Bytes, 6 );
            packet.Bytes[8] = pos.R;
            packet.Bytes[9] = pos.L;
            return packet;
        }


        public static Packet MakeSelfTeleport( Position pos ) {
            return MakeTeleport( -1, pos.GetFixed() );
        }


        public static Packet MakeMoveRotate( sbyte id, Position pos ) {
            Packet packet = new Packet( OpCode.MoveRotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)( pos.X & 0xFF );
            packet.Bytes[3] = (byte)( pos.Z & 0xFF );
            packet.Bytes[4] = (byte)( pos.Y & 0xFF );
            packet.Bytes[5] = pos.R;
            packet.Bytes[6] = pos.L;
            return packet;
        }


        public static Packet MakeMove( sbyte id, Position pos ) {
            Packet packet = new Packet( OpCode.Move );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)pos.X;
            packet.Bytes[3] = (byte)pos.Z;
            packet.Bytes[4] = (byte)pos.Y;
            return packet;
        }


        public static Packet MakeRotate( sbyte id, Position pos ) {
            Packet packet = new Packet( OpCode.Rotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = pos.R;
            packet.Bytes[3] = pos.L;
            return packet;
        }


        public static Packet MakeRemoveEntity( sbyte id ) {
            Packet packet = new Packet( OpCode.RemoveEntity );
            packet.Bytes[1] = (byte)id;
            return packet;
        }


        public static Packet MakeKick( [NotNull] string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );

            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }


        public static Packet MakeSetPermission( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            Packet packet = new Packet( OpCode.SetPermission );
            packet.Bytes[1] = (byte)( player.Can( Permission.DeleteAdmincrete ) ? 100 : 0 );
            return packet;
        }

        #endregion


        internal static void ToNetOrder( short number, byte[] arr, int offset ) {
            arr[offset] = (byte)( ( number & 0xff00 ) >> 8 );
            arr[offset + 1] = (byte)( number & 0x00ff );
        }

        internal static void ToNetOrder(int number, [NotNull] byte[] arr, int offset)
        {
            if (arr == null) throw new ArgumentNullException("arr");
            arr[offset] = (byte)((number & 0xff000000) >> 24);
            arr[offset + 1] = (byte)((number & 0x00ff0000) >> 16);
            arr[offset + 2] = (byte)((number & 0x0000ff00) >> 8);
            arr[offset + 3] = (byte)(number & 0x000000ff);
        }

        /// <summary> Returns packet size (in bytes) for a given opCode.
        /// Size includes the opCode byte itself. </summary>
        public static int GetSize( OpCode opCode ) {
            return PacketSizes[(int)opCode];
        }


        static readonly int[] PacketSizes = {
            131,    // Handshake
            1,      // Ping
            1,      // MapBegin
            1028,   // MapChunk
            7,      // MapEnd
            9,      // SetBlockClient
            8,      // SetBlockServer
            74,     // AddEntity
            10,     // Teleport
            7,      // MoveRotate
            5,      // Move
            4,      // Rotate
            2,      // RemoveEntity
            66,     // Message
            65,     // Kick
            2,      // SetPermission
            // CPE LEVEL 1
            67,     // ExtInfo
            69,     // ExtEntry
            3, // Set block range packet
            2,      // CustomBlockSupportLevel
            3, // Heldblock packet
            134, // Set Text Hotkey
            196, // Ext add playername
            130, // Ext add entity
            3, // ExtRemovePlayername packet
            8, // Env set color packet 
            87, // Make selection packet
            2, // Remove selection
            4, // SetBlockPermission
            66, // SetModel
            69 // EnvMapAppearance
        };
    }
}