using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Photo.Models;

namespace Photo.Commands
{
    // 스티커 추가 명령
    public class AddStickerCommand : ICommand
    {
        private List<StickerObject> _stickerList; // 전체 스티커 관리 리스트
        private StickerObject _mySticker;         // 내가 추가할 스티커 객체
        private Action _refreshAction;            // 화면을 새로고침할 메서드를 담을 변수

        // 생성자: 필요한 정보를 미리 받아둡니다.
        public AddStickerCommand(List<StickerObject> list, StickerObject sticker, Action refreshAction)
        {
            _stickerList = list;
            _mySticker = sticker;
            _refreshAction = refreshAction;
        }

        public void Execute()
        {
            _stickerList.Add(_mySticker); // 리스트에 추가 (화면에 나타남)
            _refreshAction?.Invoke(); // 리스트에 추가 후 화면 새로고침 실행!
        }

        public void UnExecute()
        {
            _stickerList.Remove(_mySticker); // 리스트에서 제거 (화면에서 사라짐)
            _refreshAction?.Invoke(); // 리스트에서 제거 후 화면 새로고침 실행!
        }
    }

    // 스티커 이동 명령
    public class MoveStickerCommand : ICommand
    {
        private StickerObject _sticker;
        private RectangleF _oldBounds; // 이동 전 좌표와 크기
        private RectangleF _newBounds; // 이동 후 좌표와 크기
        private Action _refresh;       // 명령이 실행될 때 화면을 새로고침

        public MoveStickerCommand(StickerObject sticker, RectangleF oldBounds, RectangleF newBounds, Action refresh)
        {
            _sticker = sticker;
            _oldBounds = oldBounds;
            _newBounds = newBounds;
            _refresh = refresh;
        }

        public void Execute()
        {
            _sticker.Bounds = _newBounds; // 새 위치로 이동
            _refresh?.Invoke();
        }

        public void UnExecute()
        {
            _sticker.Bounds = _oldBounds; // 예전 위치로 복구
            _refresh?.Invoke();
        }
    }

    // 스티커 크기 조절 명령
    public class ResizeStickerCommand : ICommand
    {
        private StickerObject _sticker;
        private RectangleF _oldBounds, _newBounds;
        private Action _refresh;

        public ResizeStickerCommand(StickerObject sticker, RectangleF old, RectangleF @new, Action refresh)
        {
            _sticker = sticker; _oldBounds = old; _newBounds = @new; _refresh = refresh;
        }

        public void Execute() { _sticker.Bounds = _newBounds; _refresh?.Invoke(); }
        public void UnExecute() { _sticker.Bounds = _oldBounds; _refresh?.Invoke(); }
    }

    // 스티커 삭제 명령
    public class DeleteStickerCommand : ICommand
    {
        private List<StickerObject> _stickerList;
        private StickerObject _targetSticker;
        private Action _refreshAction;

        public DeleteStickerCommand(List<StickerObject> list, StickerObject targetSticker, Action refreshAction)
        {
            _stickerList = list;
            _targetSticker = targetSticker;
            _refreshAction = refreshAction;
        }

        public void Execute()
        {
            _stickerList.Remove(_targetSticker); // 리스트에서 제거
            _refreshAction?.Invoke();
        }

        public void UnExecute()
        {
            _stickerList.Add(_targetSticker); // 다시 리스트에 추가
            _refreshAction?.Invoke();
        }
    }

    // 맨 앞으로 가져오기 명령
    public class BringToFrontCommand : ICommand
    {
        private List<StickerObject> _list;
        private StickerObject _sticker;
        private int _oldIndex;  // 원래 리스트에서의 위치
        private Action _refresh;

        public BringToFrontCommand(List<StickerObject> list, StickerObject sticker, Action refresh)
        {
            _list = list;
            _sticker = sticker;
            _oldIndex = list.IndexOf(sticker);
            _refresh = refresh;
        }

        public void Execute()
        {
            _list.Remove(_sticker);
            _list.Add(_sticker);  // 리스트의 맨 뒤로 보내서 가장 나중에 그려지게 함
            _refresh?.Invoke();
        }

        public void UnExecute()
        {
            _list.Remove(_sticker);
            _list.Insert(_oldIndex, _sticker);  // 원래 순서로 복구
            _refresh?.Invoke();
        }
    }

    // 투명도 변경 명령
    public class OpacityCommand : ICommand
    {
        private StickerObject _sticker;
        private float _oldOpacity, _newOpacity;
        private Action _refresh;

        public OpacityCommand(StickerObject sticker, float oldVal, float newVal, Action refresh)
        {
            _sticker = sticker;
            _oldOpacity = oldVal;
            _newOpacity = newVal;
            _refresh = refresh;
        }

        public void Execute()
        {
            _sticker.Opacity = _newOpacity;
            _refresh?.Invoke();
        }

        public void UnExecute()
        {
            _sticker.Opacity = _oldOpacity;
            _refresh?.Invoke();
        }
    }
}
