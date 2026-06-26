using System;

namespace Photo
{
    // 모든 편집 명령이 공통으로 구현하는 인터페이스.
    // 실제 Undo/Redo 스택 관리는 Core/CommandManager.cs(Main.CommandManager) 하나만 사용한다.
    public interface ICommand
    {
        void Execute();
        void UnExecute();
    }
}
