# Photo Merged

Windows Forms 기반의 사진 편집 프로그램입니다.  
이미지 불러오기부터 필터 적용, 콜라주 구성, 스티커와 텍스트 편집, 저장까지 한 화면에서 이어서 작업할 수 있도록 만들었습니다.

## 프로젝트 개요

Photo Merged는 데스크톱 환경에서 가볍게 사용할 수 있는 이미지 편집 도구를 목표로 제작했습니다.  
단순히 필터를 적용하는 기능에 그치지 않고, 편집 작업을 명령 단위로 관리해 실행 취소와 다시 실행이 가능하도록 구성했습니다.

## 주요 기능

- 이미지 파일 불러오기 및 PNG, JPEG, BMP 형식 저장
- 밝기/대비 조절
- 모자이크, 자르기, 마스크 브러시 편집
- 스티커 추가, 이동, 복사, 붙여넣기, 삭제, 투명도 조절
- 텍스트 입력 및 편집
- 1~4분할 콜라주 레이아웃 구성
- 펜과 지우개를 이용한 직접 그리기
- Undo/Redo 기반 편집 흐름
- 라이트 모드와 다크 모드 전환

## 이미지 필터

다음과 같은 필터를 직접 적용할 수 있습니다.

- Gaussian Blur
- Motion Blur
- Radial Blur
- Grayscale
- Emboss
- Find Edges
- Oil Paint
- Posterize
- Soft Glow
- Smart Sharpen
- Unsharp Mask
- Swirl
- Spherize
- Pinch
- Shear
- Wind
- Watercolor
- Color Halftone
- Crystallize
- Lens Flare
- Warm Tone / Cool Tone

## 사용 기술

- C#
- .NET 6
- Windows Forms
- OpenCvSharp
- DlibDotNet
- System.Drawing

## 구조

```text
Commands/   편집 동작을 명령 객체로 분리한 영역
Core/       명령 관리와 편집 상태 관리
Filter/     이미지 필터 처리 로직
Models/     콜라주 프레임, 스티커 객체 등 데이터 모델
Services/   이미지 저장, 콜라주, 드로잉, 줌 관련 기능
Resources/  프로그램에서 사용하는 이미지 리소스
```

## 구현 포인트

### 명령 패턴 기반 편집 관리

필터 적용, 스티커 추가/삭제, 투명도 변경 같은 작업을 명령 단위로 분리했습니다.  
이 구조를 통해 여러 편집 기능이 같은 Undo/Redo 흐름을 사용할 수 있게 했습니다.

### 저장 시 레이어 병합

작업 중인 스티커와 텍스트는 화면 위에 별도로 관리하고, 저장 시 원본 이미지 위에 병합해 최종 이미지를 생성합니다.  
편집 중에는 각 요소를 독립적으로 조작할 수 있고, 저장 결과에는 하나의 이미지로 반영됩니다.

### 기능별 책임 분리

필터, 드로잉, 콜라주, 파일 입출력 기능을 각각 별도 클래스와 서비스로 나누었습니다.  
기능을 추가하거나 수정할 때 한 파일에 모든 로직이 몰리지 않도록 구조를 정리했습니다.

## 실행 방법

Windows 환경에서 Visual Studio로 `Photo.slnx` 또는 `Photo.csproj`를 열어 실행합니다.

필요한 환경:

- Windows
- Visual Studio 2022 이상
- .NET 6 SDK

## 시연 영상

[![Photo Merged 시연 영상](https://img.youtube.com/vi/-YuLMFe86JE/hqdefault.jpg)](https://youtu.be/-YuLMFe86JE)

영상 링크: https://youtu.be/-YuLMFe86JE
