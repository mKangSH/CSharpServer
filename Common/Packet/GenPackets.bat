IF NOT EXIST ../../PacketGenerator/bin/Debug/PacketGenerator.exe GOTO DEBUGNOTEXIST
START ../../PacketGenerator/bin/Debug/PacketGenerator.exe ../../PacketGenerator/PDL.xml
GOTO EXECUTE

:DEBUGNOTEXIST
IF NOT EXIST ../../PacketGenerator/bin/Release/PacketGenerator.exe GOTO DONE
START ../../PacketGenerator/bin/Release/PacketGenerator.exe ../../PacketGenerator/PDL.xml
GOTO EXECUTE

:EXECUTE
XCOPY /Y GenPackets.cs "../../DummyClient/Packet"
XCOPY /Y GenPackets.cs "../../CSharpServer/Packet"

XCOPY /Y ClientPacketManager.cs "../../DummyClient/Packet"
XCOPY /Y ServerPacketManager.cs "../../CSharpServer/Packet"
:DONE