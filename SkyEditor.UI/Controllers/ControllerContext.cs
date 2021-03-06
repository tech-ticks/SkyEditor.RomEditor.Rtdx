using SkyEditor.RomEditor.Domain.Rtdx.Constants;
using SkyEditorUI.Infrastructure;

namespace SkyEditorUI.Controllers
{
    public abstract class ControllerContext
    {
        public static ControllerContext Null { get; } = new NullControllerContext();
    }

    public class NullControllerContext : ControllerContext
    {
    }

    public class SourceFileControllerContext : ControllerContext
    {
        public SourceFile SourceFile { get; }

        public SourceFileControllerContext(SourceFile sourceFile)
        {
            SourceFile = sourceFile;
        }
    }

    public class DungeonControllerContext : ControllerContext
    {
        public DungeonIndex Index { get; }

        public DungeonControllerContext(DungeonIndex index)
        {
            Index = index;
        }
    }

    public class DungeonFloorControllerContext : ControllerContext
    {
        public DungeonIndex DungeonIndex { get; }
        public int FloorIndex { get; set; }

        public DungeonFloorControllerContext(DungeonIndex dungeonIndex, int floorNum)
        {
            DungeonIndex = dungeonIndex;
            FloorIndex = floorNum;
        }
    }
}
