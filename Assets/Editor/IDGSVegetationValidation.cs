#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusQuest.IDGS;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

namespace CampusQuest.EditorTools
{
    /// <summary>Explicit test-scene-only physics, import and rendered occlusion checks.</summary>
    public static class IDGSVegetationValidation
    {
        public static string Status { get; private set; } = "Not started";
        private static IEnumerator steps;
        private static int frame;
        private static readonly List<string> results = new List<string>();
        private static IDGSVegetationTestPlayer player;
        private static Rigidbody2D body;
        private static Keyboard keyboard;
        private static Keyboard previousKeyboard;
        private static bool originalBackground;
        private static IDGSVegetationYSort[] trees;
        private static bool largeTrees;

        public static void BeginLargeTrees() { largeTrees = true; Begin(); }

        public static void Begin()
        {
            if (!Application.isPlaying || steps != null || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != IDGSVegetationSetup.ScenePath)
                throw new InvalidOperationException("Run only in the independent IDGS vegetation test scene in Play Mode.");
            results.Clear(); Status = "Running"; frame = -1;
            player = UnityEngine.Object.FindFirstObjectByType<IDGSVegetationTestPlayer>(); body = player.GetComponent<Rigidbody2D>();
            trees = UnityEngine.Object.FindObjectsByType<IDGSVegetationYSort>(FindObjectsSortMode.None).Where(s => s.name.StartsWith("Arbol_", StringComparison.Ordinal) && !s.name.StartsWith("Arbol_Grande_", StringComparison.Ordinal)).OrderBy(t => t.transform.position.x).ToArray();
            if (largeTrees) trees = UnityEngine.Object.FindObjectsByType<IDGSVegetationYSort>(FindObjectsSortMode.None).Where(s => s.name.StartsWith("Arbol_Grande_", StringComparison.Ordinal)).OrderBy(t => t.transform.position.x).ToArray();
            previousKeyboard = Keyboard.current; keyboard = InputSystem.AddDevice<Keyboard>("IDGSVegetationValidationKeyboard"); keyboard.MakeCurrent();
            originalBackground = Application.runInBackground; Application.runInBackground = true; EditorApplication.isPaused = false;
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            EditorWindow.GetWindow(gameViewType).Show();
            steps = Validate(); EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            if (!Application.isPlaying) { Finish("FAILED: Play Mode ended"); return; }
            EditorApplication.QueuePlayerLoopUpdate();
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            try { if (!steps.MoveNext()) Finish("PASS"); }
            catch (Exception e) { Finish("FAILED: " + e); }
        }
        private static void Finish(string status)
        {
            EditorApplication.update -= Tick; steps = null; Status = status;
            Release();
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            Application.runInBackground = originalBackground;
            string folder = largeTrees ? "tmp/idgs-large-trees" : "tmp/idgs-vegetation";
            Directory.CreateDirectory(folder); File.WriteAllText(folder + "/results.txt", string.Join("\n", results) + "\n" + status); largeTrees = false;
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message); results.Add("PASS: " + message);
        }
        private static void Hold(Key key)
        {
            keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update(); player.SendMessage("Update");
        }
        private static void Release()
        {
            if (keyboard != null && keyboard.added) InputSystem.ResetDevice(keyboard);
            if (player != null) player.SendMessage("Update");
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        private static IEnumerable<object> Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < end) yield return null;
        }
        private static void Teleport(Vector2 point)
        {
            Release(); body.position = point; body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
        }
        private static IEnumerable<object> WalkTo(Vector2 point)
        {
            float end = Time.realtimeSinceStartup + 8;
            while (Vector2.Distance(body.position, point) > .10f && Time.realtimeSinceStartup < end)
            {
                Vector2 delta = point - body.position;
                Hold(Mathf.Abs(delta.x) > .08f ? delta.x > 0 ? Key.D : Key.A : delta.y > 0 ? Key.W : Key.S);
                yield return null;
            }
            Release();
            Check(Vector2.Distance(body.position, point) < .20f, "Actual input/physics movement reaches " + point);
            yield return null;
        }

        private static IEnumerator Validate()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(largeTrees ? IDGSLargeVegetationSetup.SheetPath : IDGSVegetationSetup.SpritePath);
            Check(importer.spriteImportMode == SpriteImportMode.Multiple && importer.spritePixelsPerUnit == 32
                && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed,
                "Import uses Multiple, 32 PPU, Point, no mipmaps, no compression");
            var sprites = AssetDatabase.LoadAllAssetsAtPath(IDGSVegetationSetup.SpritePath).OfType<Sprite>().ToArray();
            Check(sprites.Length == 64 && sprites.All(s => s.rect.width == 32 && s.rect.height == 32 && s.pixelsPerUnit == 32), "All 64 exact 32x32 slices preserved");
            Check(sprites.Count(s => s.name.EndsWith("_Vacia", StringComparison.Ordinal)) == 1
                && UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).All(r => r.sprite == null || !r.sprite.name.EndsWith("_Vacia", StringComparison.Ordinal)),
                "Last empty slice remains in asset and creates no scene object");
            Check(AssetDatabase.FindAssets("t:Prefab", new[] { IDGSVegetationSetup.PrefabsPath }).Count(g => !AssetDatabase.GUIDToAssetPath(g).Contains("/ArbolesGrandes/")) == 6, "Original six vegetation prefabs remain intact");
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { IDGSVegetationSetup.PrefabsPath }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                Check(prefab.GetComponentsInChildren<Transform>(true).All(t => t.localScale == Vector3.one), prefab.name + " preserves original scale on every transform");
            }
            Check(trees.Length == (largeTrees ? 4 : 3) && trees.All(t => t.GetComponent<BoxCollider2D>() != null), "Every tree root has an independent small base collider");
            if (largeTrees) {
                var bigSprites = AssetDatabase.LoadAllAssetsAtPath(IDGSLargeVegetationSetup.SheetPath).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
                Check(bigSprites.Length == 4 && bigSprites.All(s=>s.rect.height >=105 && s.rect.width>=96 && s.pixelsPerUnit==32 && s.pivot.y==0), "Four complete native-scale trees with foot pivots and no canopy cuts");
            }
            foreach (var tree in trees)
            {
                var colliders = tree.GetComponentsInChildren<Collider2D>(); var box = tree.GetComponent<BoxCollider2D>();
                Check(colliders.Length == 1 && (largeTrees ? box.size.x <=22f/32 && box.size.y==10f/32 && box.offset==new Vector2(0,5f/32) : box.size == new Vector2(8f / 32, 6f / 32) && box.offset == new Vector2(0, 3f / 32)), tree.name + " collision occupies only the trunk base");
                var canopy = tree.transform.Find(largeTrees ? "Arbol_Completo" : "Copa");
                Check(canopy.GetComponent<Collider2D>() == null, tree.name + " canopy has no collider");
                Vector2 crownCenter = canopy.GetComponent<SpriteRenderer>().bounds.center;
                Check(Physics2D.OverlapPoint(crownCenter) == null, tree.name + " canopy center is physically free");
                var root = (Vector2)tree.transform.position;
                Teleport(root + new Vector2(0, -.65f)); yield return null;
                float contactDeadline = Time.realtimeSinceStartup + 8;
                while (!player.GetComponent<Collider2D>().IsTouching(box) && Time.realtimeSinceStartup < contactDeadline)
                { Hold(Key.W); yield return null; }
                Release();
                Check(body.position.y < root.y - .10f && body.position.y > root.y - .4f, tree.name + " stops Mateo approaching trunk from below");
                Check(player.GetComponent<Collider2D>().IsTouching(box), tree.name + " contacts real Rigidbody2D/Collider2D");
                Teleport(root + new Vector2(-.65f, -.4f)); yield return null;
                foreach (var target in new[] { root + new Vector2(-.65f, .65f), root + new Vector2(.65f, .65f), root + new Vector2(.65f, -.4f), root + new Vector2(0, -.4f) })
                    foreach (var step in WalkTo(target)) yield return step;
                Check(true, tree.name + " can be circled on both sides and walked behind the canopy");
            }
            var shrubs = UnityEngine.Object.FindObjectsByType<IDGSVegetationYSort>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Arbusto_", StringComparison.Ordinal));
            Check(shrubs.All(t => t.GetComponentsInChildren<Collider2D>().Length == 0), "Low bushes remain decorative and passable");
            Teleport(new Vector2(5.8f, -.5f));
            keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.D)); InputSystem.Update(); player.SendMessage("Update");
            Check(Mathf.Abs(player.RequestedVelocity.magnitude - 6) < .001f, "Shift retains 6-unit test running speed");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); InputSystem.Update(); player.SendMessage("Update");
            Check(Mathf.Abs(player.RequestedVelocity.magnitude - 4) < .001f, "Releasing Shift returns test movement to 4 units"); Release();
            yield return null;
            foreach (var tree in largeTrees ? trees : new[]{trees[1]}) RenderOcclusionProof(tree);
            Teleport(new Vector2(-2, -.5f)); yield return null;
            Capture(Camera.main, "tmp/idgs-vegetation/overview.png", 1280, 720);
            var renderers = trees[1].GetComponentsInChildren<SpriteRenderer>();
            var bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            results.Add("MEASURE: tree full-rect bounds=" + bounds.size + "; actor full-rect bounds=" + player.GetComponentInChildren<SpriteRenderer>().bounds.size + "; no transform scale changes");
            Check(!EditorUtility.scriptCompilationFailed, "Project compiles without errors");
        }

        private static int Difference(Color32[] a, Color32[] b)
        {
            int count = 0;
            for (int i = 0; i < a.Length; i++) if (Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b) > 12) count++;
            return count;
        }
        private static void RenderOcclusionProof(IDGSVegetationYSort tree)
        {
            Release(); var camera = Camera.main; var oldPosition = camera.transform.position; float oldSize = camera.orthographicSize;
            var oldActor = body.position; bool simulated = body.simulated; bool enabled = player.enabled;
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(); bool canvasEnabled = canvas.enabled;
            var actorRenderer = player.GetComponentInChildren<SpriteRenderer>(); var treeRenderers = tree.GetComponentsInChildren<SpriteRenderer>();
            var actorSort = player.GetComponent<IDGSVegetationYSort>(); var animator = player.GetComponentInChildren<Animator>();
            try
            {
                player.enabled = false; body.simulated = false; canvas.enabled = false;
                camera.transform.position = new Vector3(tree.transform.position.x, tree.transform.position.y + (largeTrees ? 1.4f : .65f), -10); camera.orthographicSize = largeTrees ? 2.3f : 1.35f;
                animator.SetBool("IsMoving", false); player.GetComponentInChildren<CampusQuestMateoVisualDriver>().Face(Vector2Int.down); animator.Play("Idle_Down", 0, 0); animator.Update(0);
                tree.Refresh();
                foreach (bool behind in new[] { true, false })
                {
                    player.transform.position = tree.transform.position + new Vector3(0, behind ? (largeTrees ? 1.4f : .35f) : -.35f, 0); actorSort.Refresh();
                    bool order = behind ? actorSort.GetComponent<SortingGroup>().sortingOrder < tree.GetComponent<SortingGroup>().sortingOrder
                        : actorSort.GetComponent<SortingGroup>().sortingOrder > tree.GetComponent<SortingGroup>().sortingOrder;
                    Check(order, behind ? "Actor group sorts behind tree by feet Y" : "Actor group sorts in front of tree by feet Y");
                    foreach (var r in treeRenderers) r.enabled = false; actorRenderer.enabled = false;
                    var background = Capture(camera, null, 640, 640); actorRenderer.enabled = true;
                    var actorOnly = Capture(camera, null, 640, 640); foreach (var r in treeRenderers) r.enabled = true; actorRenderer.enabled = false;
                    var treeOnly = Capture(camera, null, 640, 640); actorRenderer.enabled = true;
                    var combined = Capture(camera, (largeTrees ? "tmp/idgs-large-trees/" + tree.name + "-" : "tmp/idgs-vegetation/") + (behind ? "mateo-detras.png" : "mateo-delante.png"), 640, 640);
                    int full = Difference(actorOnly, background), visible = Difference(combined, treeOnly);
                    float fraction = full == 0 ? 0 : (float)visible / full;
                    Check(full > 100 && (behind ? fraction < .8f : fraction > .95f), (behind ? "Rendered canopy hides Mateo" : "Rendered Mateo appears over vegetation")
                        + "; visible pixels=" + visible + "/" + full + " (" + fraction.ToString("P0") + ")");
                }
            }
            finally
            {
                foreach (var r in treeRenderers) r.enabled = true; actorRenderer.enabled = true; canvas.enabled = canvasEnabled;
                camera.transform.position = oldPosition; camera.orthographicSize = oldSize; body.simulated = simulated;
                body.position = oldActor; player.transform.position = oldActor; player.enabled = enabled; Physics2D.SyncTransforms();
            }
        }

        public static Color32[] Capture(Camera camera, string path, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prior = RenderTexture.active; var target = camera.targetTexture; float aspect = camera.aspect;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = rt; camera.aspect = (float)width / height; Canvas.ForceUpdateCanvases();
                // These proof captures enable/disable renderers inside one Editor update.
                // Refresh native group membership before each manual camera render.
                SortingGroup.UpdateAllSortingGroups(); camera.Render(); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                if (path != null) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, texture.EncodeToPNG()); }
                return texture.GetPixels32();
            }
            finally { camera.targetTexture = target; camera.aspect = aspect; RenderTexture.active = prior; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
#endif
