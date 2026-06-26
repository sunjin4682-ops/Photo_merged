using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photo.Commands;


// -----------------------------------------------------------------------------
// 파일 역할: Undo/Redo 스택과 명령 실행 흐름을 관리한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        // 명령 실행과 Undo/Redo 스택 관리를 담당하는 관리자 클래스.
        internal class CommandManager //스택 기반 Undo/redo의 본체
        {
            private readonly Stack<ICommand> undoStack = new Stack<ICommand>();
            private readonly Stack<ICommand> redoStack = new Stack<ICommand>();

            public bool CanUndo => undoStack.Count > 0;
            public bool CanRedo => redoStack.Count > 0;

            /// <summary>
            /// 새 편집 명령을 실행하고 Undo 스택에 저장한다.
            /// 새 작업이 발생하면 예전 Redo 경로는 더 이상 유효하지 않으므로 비운다.
            /// </summary>
            public void ExecuteCommand(ICommand command)
            {
                if (command == null) return;

                // 명령 객체가 내부적으로 이미지 변경/상태 저장을 수행한다.
                command.Execute();
                // 방금 실행한 작업은 되돌릴 수 있어야 하므로 undoStack에 넣는다.
                undoStack.Push(command);
                // 새 분기가 생겼으므로 이전 redo 기록은 모두 무효화된다.
                redoStack.Clear();
            }

            /// <summary>
            /// 가장 최근 명령을 취소한다.
            /// 실행 취소된 명령은 redoStack으로 옮겨 다시 살릴 수 있게 한다.
            /// </summary>
            public void Undo()
            {
                if (undoStack.Count == 0) return;

                ICommand command = undoStack.Pop();
                command.UnExecute();
                redoStack.Push(command);
            }

            /// <summary>
            /// 방금 취소한 명령을 다시 실행한다.
            /// redoStack에서 꺼낸 뒤 다시 undoStack으로 되돌린다.
            /// </summary>
            public void Redo()
            {
                if (redoStack.Count == 0) return;

                ICommand command = redoStack.Pop();
                command.Execute();
                undoStack.Push(command);
            }

            /// <summary>
            /// 작업 이력을 모두 초기화한다.
            /// 새 이미지를 열 때 이전 이미지의 Undo/Redo 기록이 섞이지 않게 할 때 사용한다.
            /// </summary>
            public void Clear()
            {
                undoStack.Clear();
                redoStack.Clear();
            }
        }
    }
}
