using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Void2610.CinematicEffect
{
    /// <summary>
    /// CinematicEffect の <see cref="ScreenFadeEffect"/> / <see cref="ImageFlashEffect"/> 等が利用する
    /// 全画面オーバーレイ Image を、 シーン上に事前配置することなくコードから宣言的に自動生成する Singleton。
    ///
    /// Effect が初めて呼ばれた瞬間に Canvas (ScreenSpaceOverlay / sortingOrder=short.MaxValue) と
    /// 全画面ストレッチの透明 Image が一括構築される。 各シーン専用に生きるため、 シーン遷移ごとに作り直される。
    /// </summary>
    public sealed class CinematicOverlay : CinematicSingletonBehaviour<CinematicOverlay>
    {
        private Image _image;
        private Canvas _canvas;
        private Image _cameraImage;
        private Canvas _cameraCanvas;

        /// <summary>フェード / フラッシュの塗りつぶし対象となる全画面 Image。</summary>
        public Image Image
        {
            get
            {
                if (_image == null) BuildOverlay();
                return _image;
            }
        }

        /// <summary>レターボックス帯など子オブジェクトの親にする、このオーバーレイの Canvas。</summary>
        public Canvas Canvas
        {
            get
            {
                if (_image == null) BuildOverlay();
                return _canvas;
            }
        }

        /// <summary>
        /// ポストプロセスの影響を受けたい画像表示用の全画面 Image (ScreenSpaceCamera)。
        /// ScreenSpaceOverlay の <see cref="Image"/> はポスト処理の後段に描かれるため、
        /// グレイン / ビネット等を画像にも掛けたい場合はこちらを使う。 Camera.main が無ければ <see cref="Image"/> にフォールバック。
        /// </summary>
        public Image CameraSpaceImage
        {
            get
            {
                var camera = Camera.main;
                if (camera == null) return Image;
                if (_cameraImage == null) BuildCameraOverlay();
                _cameraCanvas.worldCamera = camera;
                // 手前の世界オブジェクトに遮蔽されないよう、可能な限りカメラ近くに置く
                _cameraCanvas.planeDistance = camera.nearClipPlane + 0.1f;
                return _cameraImage;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            // シーンごとに使う前提 (DontDestroyOnLoad しない)。 ルート扱いされて DDOL が付くケースをここで打ち消す
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
            BuildOverlay();
        }

        private void BuildOverlay()
        {
            if (_image != null) return;

            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue; // 通常 UI より常に手前
            _canvas = canvas;
            if (gameObject.GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();

            var imgGo = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imgGo.transform.SetParent(transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _image = imgGo.GetComponent<Image>();
            _image.color = new Color(0f, 0f, 0f, 0f);
            _image.raycastTarget = false;
        }

        private void BuildCameraOverlay()
        {
            // ネストした Canvas は renderMode を上書きできないため、オーバーレイ Canvas の子ではなく独立ルートに作る
            var go = new GameObject("CinematicOverlayCameraSpace", typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            _cameraCanvas = go.GetComponent<Canvas>();
            _cameraCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _cameraCanvas.sortingOrder = short.MaxValue;

            var imgGo = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imgGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _cameraImage = imgGo.GetComponent<Image>();
            _cameraImage.color = new Color(0f, 0f, 0f, 0f);
            _cameraImage.raycastTarget = false;
        }
    }
}
