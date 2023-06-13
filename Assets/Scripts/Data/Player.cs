using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct Player
{
    public int q, r;
    public int actions, moves;
}