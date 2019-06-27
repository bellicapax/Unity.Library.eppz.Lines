//
// Copyright (c) 2017 Geri Borbás http://www.twitter.com/_eppz
// Modifications by Eris Koleszar http://eris.lol.
// Debug Extension drawing methods from Arkham Interactive's Debug Draw Extension Asset Store package
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//  The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DebugDraw : MonoBehaviour
{
    [System.Serializable]
    public struct Line
    {
        public Vector3 From;
        public Vector3 To;
        public Color Color;
    }

    #region Singleton Implementation
    // Check to see if we're about to be destroyed.
    private static bool _shuttingDown = false;
    private static object _lock = new object();
    private static DebugDraw _instance;

    /// <summary>
    /// Access singleton instance through this propriety.
    /// </summary>
    public static DebugDraw Instance
    {
        get
        {
            if (_shuttingDown)
            {
                Debug.LogWarning("[Singleton] Instance already destroyed. Returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // Search for existing instance.
                    _instance = FindObjectOfType<DebugDraw>();

                    // Create new instance if one doesn't already exist.
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<DebugDraw>();
                        if (_instance == null)
                        {
                            // First try to add it to the main camera so we don't add unnecessary cameras
                            var cam = Camera.main;
                            if (cam != null)
                            {
                                _instance = cam.gameObject.AddComponent<DebugDraw>();
                            }
                            else
                            {
                                // Need to create a new GameObject to attach the singleton to.
                                var singletonObject = new GameObject();
                                _instance = singletonObject.AddComponent<DebugDraw>();
                                singletonObject.name = "DebugDraw (Singleton)";
                            }
                        }
                    }
                }
                return _instance;
            }
        }
    }


    private void OnApplicationQuit()
    {
        _shuttingDown = true;
    }


    private void OnDestroy()
    {
        _shuttingDown = true;
    }

    #endregion

    private Camera _camera;

    private List<Line> _lineBatch = new List<Line>();
    [SerializeField]
    private Material _material;
    private bool _ranPostRender;

    private void Reset()
    {
        _camera = GetComponent<Camera>();
    }

    private void Awake()
    {
        _instance = this;
        _camera = GetComponent<Camera>();
        if (_material == null)
            _material = new Material(Shader.Find("GUI/Text Shader"));
    }

    #region Draw in Scene View

#if UNITY_EDITOR

    private void LateUpdate()
    {
        DrawLines();
    }

    private void DrawLines()
    {
        // NEED TO CLEAR IF IN SCENE VIEWWWWW
        var numLines = _lineBatch.Count;
        for (int i = 0; i < numLines; i++)
        {
            Debug.DrawLine(_lineBatch[i].From, _lineBatch[i].To, _lineBatch[i].Color);
        }

        if (_ranPostRender)
        {
            _ranPostRender = false;
        }
        else
        {
            _lineBatch.Clear();
        }
    }
#endif

    #endregion

    #region Draw in Game View

    void OnPostRender()
    {
        _ranPostRender = true;
        GL.PushMatrix();
        GL.LoadProjectionMatrix(_camera.projectionMatrix);
        DrawCall();
        GL.PopMatrix();

        _lineBatch.Clear();
    }

    private void DrawCall()
    {
        // Assign vertex color material.
        _material.SetPass(0); // Single draw call (set pass call)

        // Send vertices in GL_LINES Immediate Mode.
        GL.Begin(GL.LINES);
        foreach (var line in _lineBatch)
        {
            GL.Color(line.Color);
            GL.Vertex(line.From);
            GL.Vertex(line.To);
        }
        GL.End();
    }

    #endregion

    #region EPPZ Drawing Methods

    public void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        _lineBatch.Add(new Line()
        {
            From = from,
            To = to,
            Color = color
        });
    }

    public void DrawLineWithTransform(Vector2 from, Vector2 to, Color color, Transform transform_)
    {
        Vector2 from_ = transform_.TransformPoint(from);
        Vector2 to_ = transform_.TransformPoint(to);
        DrawLine(from_, to_, color);
    }

    public void DrawConnectedPoints(Vector2[] points, Color color, bool closed = true)
    { DrawConnectedPointsWithTransform(points, color, null, closed); }

    public void DrawConnectedPointsWithTransform(Vector2[] points, Color color, Transform transform_, bool closed = true)
    {
        int lastIndex = (closed) ? points.Length : points.Length - 1;
        bool useTransform = transform_ != null;
        for (int index = 0; index < lastIndex; index++)
        {
            Vector2 eachPoint = points[index];
            Vector2 eachNextPoint = (index < points.Length - 1) ? points[index + 1] : points[0];

            // Apply shape transform (if any).
            if (useTransform)
            {
                eachPoint = transform_.TransformPoint(eachPoint);
                eachNextPoint = transform_.TransformPoint(eachNextPoint);
            }

            // Draw.
            DrawLine(eachPoint, eachNextPoint, color);
        }
    }

    public void DrawRect(Rect rect, Color color)
    { DrawRectWithTransform(rect, color, null); }

    public void DrawRectWithTransform(Rect rect, Color color, Transform transform_)
    {
        Vector2 leftTop = new Vector2(rect.xMin, rect.yMin);
        Vector2 rightTop = new Vector2(rect.xMax, rect.yMin);
        Vector2 rightBottom = new Vector2(rect.xMax, rect.yMax);
        Vector2 leftBottom = new Vector2(rect.xMin, rect.yMax);

        if (transform_ != null)
        {
            leftTop = transform_.TransformPoint(leftTop);
            rightTop = transform_.TransformPoint(rightTop);
            rightBottom = transform_.TransformPoint(rightBottom);
            leftBottom = transform_.TransformPoint(leftBottom);
        }

        DrawLine(
            leftTop,
            rightTop,
            color);

        DrawLine(
            rightTop,
            rightBottom,
            color);

        DrawLine(
            rightBottom,
            leftBottom,
            color);

        DrawLine(
            leftTop,
            leftBottom,
            color);
    }

    public void DrawCircle(Vector2 center, float radius, int segments, Color color)
    { DrawCircleWithTransform(center, radius, segments, color, null); }

    public void DrawCircleWithTransform(Vector2 center, float radius, int segments, Color color, Transform transform_)
    {
        Vector2[] vertices = new Vector2[segments];

        // Compose a half circle (and mirrored) in normalized space (at 0,0).
        float angularStep = Mathf.PI * 2.0f / (float)segments;
        for (int index = 0; index < 1 + segments / 2; index++)
        {
            // Trigonometry.
            float angle = (float)index * angularStep;
            float x = Mathf.Sin(angle);
            float y = Mathf.Cos(angle);

            Vector2 vertex = new Vector2(x * radius, y * radius);
            Vector2 mirrored = new Vector2(-x * radius, y * radius);

            // Save right, then left.
            vertices[index] = vertex;
            if (index > 0) vertices[segments - index] = mirrored;
        }

        // Draw around center.
        for (int index = 0; index < segments - 1; index++)
        {
            if (transform_ != null)
            {
                DrawLineWithTransform(
                    center + vertices[index],
                    center + vertices[index + 1],
                    color,
                    transform
                );
            }
            else
            {
                DrawLine(
                    center + vertices[index],
                    center + vertices[index + 1],
                    color
                );
            }
        }

        // Last segment.
        if (transform_ != null)
        {
            DrawLineWithTransform(
                center + vertices[segments - 1],
                center + vertices[0],
                color,
                transform
            );
        }
        else
        {
            DrawLine(
                center + vertices[segments - 1],
                center + vertices[0],
                color
            );
        }
    }

    #endregion

    #region Translation Between Drawing Methods

    // TODO: Actually implement duration and depthTest where necessary
    public void DrawRay(Vector3 origin, Vector3 dir, Color color, float duration = 0, bool depthTest = true)
    {
        DrawLine(origin, origin + dir, color);
    }

    private void DrawLine(Vector3 from, Vector3 to, Color color, float duration = 0, bool depthTest = true)
    {
        DrawLine(from, to, color);
    }

    #endregion

    #region DebugExtension Drawing Methods

    // These functions below are from the DebugExtension Unity Asset Store free package

    /// <summary>
    /// 	- Debugs a point.
    /// </summary>
    /// <param name='position'>
    /// 	- The point to debug.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the point.
    /// </param>
    /// <param name='scale'>
    /// 	- The size of the point.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the point.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not this point should be faded when behind other objects.
    /// </param>
    public void DrawPoint(Vector3 position, Color color, float scale = 1.0f, float duration = 0, bool depthTest = true)
    {
        color = (color == default(Color)) ? Color.white : color;

        DrawRay(position + (Vector3.up * (scale * 0.5f)), -Vector3.up * scale, color, duration, depthTest);
        DrawRay(position + (Vector3.right * (scale * 0.5f)), -Vector3.right * scale, color, duration, depthTest);
        DrawRay(position + (Vector3.forward * (scale * 0.5f)), -Vector3.forward * scale, color, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a point.
    /// </summary>
    /// <param name='position'>
    /// 	- The point to debug.
    /// </param>
    /// <param name='scale'>
    /// 	- The size of the point.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the point.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not this point should be faded when behind other objects.
    /// </param>
    public void DrawPoint(Vector3 position, float scale = 1.0f, float duration = 0, bool depthTest = true)
    {
        DrawPoint(position, Color.white, scale, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs an axis-aligned bounding box.
    /// </summary>
    /// <param name='bounds'>
    /// 	- The bounds to debug.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the bounds.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the bounds.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the bounds should be faded when behind other objects.
    /// </param>
    public void DrawBounds(Bounds bounds, Color color, float duration = 0, bool depthTest = true)
    {
        Vector3 center = bounds.center;

        float x = bounds.extents.x;
        float y = bounds.extents.y;
        float z = bounds.extents.z;

        Vector3 ruf = center + new Vector3(x, y, z);
        Vector3 rub = center + new Vector3(x, y, -z);
        Vector3 luf = center + new Vector3(-x, y, z);
        Vector3 lub = center + new Vector3(-x, y, -z);

        Vector3 rdf = center + new Vector3(x, -y, z);
        Vector3 rdb = center + new Vector3(x, -y, -z);
        Vector3 lfd = center + new Vector3(-x, -y, z);
        Vector3 lbd = center + new Vector3(-x, -y, -z);

        DrawLine(ruf, luf, color, duration, depthTest);
        DrawLine(ruf, rub, color, duration, depthTest);
        DrawLine(luf, lub, color, duration, depthTest);
        DrawLine(rub, lub, color, duration, depthTest);

        DrawLine(ruf, rdf, color, duration, depthTest);
        DrawLine(rub, rdb, color, duration, depthTest);
        DrawLine(luf, lfd, color, duration, depthTest);
        DrawLine(lub, lbd, color, duration, depthTest);

        DrawLine(rdf, lfd, color, duration, depthTest);
        DrawLine(rdf, rdb, color, duration, depthTest);
        DrawLine(lfd, lbd, color, duration, depthTest);
        DrawLine(lbd, rdb, color, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs an axis-aligned bounding box.
    /// </summary>
    /// <param name='bounds'>
    /// 	- The bounds to debug.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the bounds.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the bounds should be faded when behind other objects.
    /// </param>
    public void DrawBounds(Bounds bounds, float duration = 0, bool depthTest = true)
    {
        DrawBounds(bounds, Color.white, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a local cube.
    /// </summary>
    /// <param name='transform'>
    /// 	- The transform that the cube will be local to.
    /// </param>
    /// <param name='size'>
    /// 	- The size of the cube.
    /// </param>
    /// <param name='color'>
    /// 	- Color of the cube.
    /// </param>
    /// <param name='center'>
    /// 	- The position (relative to transform) where the cube will be debugged.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cube.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cube should be faded when behind other objects.
    /// </param>
    public void DrawLocalCube(Transform transform, Vector3 size, Color color, Vector3 center = default(Vector3), float duration = 0, bool depthTest = true)
    {
        Vector3 lbb = transform.TransformPoint(center + ((-size) * 0.5f));
        Vector3 rbb = transform.TransformPoint(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

        Vector3 lbf = transform.TransformPoint(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
        Vector3 rbf = transform.TransformPoint(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

        Vector3 lub = transform.TransformPoint(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
        Vector3 rub = transform.TransformPoint(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

        Vector3 luf = transform.TransformPoint(center + ((size) * 0.5f));
        Vector3 ruf = transform.TransformPoint(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

        DrawLine(lbb, rbb, color, duration, depthTest);
        DrawLine(rbb, lbf, color, duration, depthTest);
        DrawLine(lbf, rbf, color, duration, depthTest);
        DrawLine(rbf, lbb, color, duration, depthTest);

        DrawLine(lub, rub, color, duration, depthTest);
        DrawLine(rub, luf, color, duration, depthTest);
        DrawLine(luf, ruf, color, duration, depthTest);
        DrawLine(ruf, lub, color, duration, depthTest);

        DrawLine(lbb, lub, color, duration, depthTest);
        DrawLine(rbb, rub, color, duration, depthTest);
        DrawLine(lbf, luf, color, duration, depthTest);
        DrawLine(rbf, ruf, color, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a local cube.
    /// </summary>
    /// <param name='transform'>
    /// 	- The transform that the cube will be local to.
    /// </param>
    /// <param name='size'>
    /// 	- The size of the cube.
    /// </param>
    /// <param name='center'>
    /// 	- The position (relative to transform) where the cube will be debugged.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cube.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cube should be faded when behind other objects.
    /// </param>
    public void DrawLocalCube(Transform transform, Vector3 size, Vector3 center = default(Vector3), float duration = 0, bool depthTest = true)
    {
        DrawLocalCube(transform, size, Color.white, center, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a local cube.
    /// </summary>
    /// <param name='space'>
    /// 	- The space the cube will be local to.
    /// </param>
    /// <param name='size'>
    ///		- The size of the cube.
    /// </param>
    /// <param name='color'>
    /// 	- Color of the cube.
    /// </param>
    /// <param name='center'>
    /// 	- The position (relative to transform) where the cube will be debugged.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cube.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cube should be faded when behind other objects.
    /// </param>
    public void DrawLocalCube(Matrix4x4 space, Vector3 size, Color color, Vector3 center = default(Vector3), float duration = 0, bool depthTest = true)
    {
        color = (color == default(Color)) ? Color.white : color;

        Vector3 lbb = space.MultiplyPoint3x4(center + ((-size) * 0.5f));
        Vector3 rbb = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f));

        Vector3 lbf = space.MultiplyPoint3x4(center + (new Vector3(size.x, -size.y, size.z) * 0.5f));
        Vector3 rbf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f));

        Vector3 lub = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f));
        Vector3 rub = space.MultiplyPoint3x4(center + (new Vector3(size.x, size.y, -size.z) * 0.5f));

        Vector3 luf = space.MultiplyPoint3x4(center + ((size) * 0.5f));
        Vector3 ruf = space.MultiplyPoint3x4(center + (new Vector3(-size.x, size.y, size.z) * 0.5f));

        DrawLine(lbb, rbb, color, duration, depthTest);
        DrawLine(rbb, lbf, color, duration, depthTest);
        DrawLine(lbf, rbf, color, duration, depthTest);
        DrawLine(rbf, lbb, color, duration, depthTest);

        DrawLine(lub, rub, color, duration, depthTest);
        DrawLine(rub, luf, color, duration, depthTest);
        DrawLine(luf, ruf, color, duration, depthTest);
        DrawLine(ruf, lub, color, duration, depthTest);

        DrawLine(lbb, lub, color, duration, depthTest);
        DrawLine(rbb, rub, color, duration, depthTest);
        DrawLine(lbf, luf, color, duration, depthTest);
        DrawLine(rbf, ruf, color, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a local cube.
    /// </summary>
    /// <param name='space'>
    /// 	- The space the cube will be local to.
    /// </param>
    /// <param name='size'>
    ///		- The size of the cube.
    /// </param>
    /// <param name='center'>
    /// 	- The position (relative to transform) where the cube will be debugged.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cube.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cube should be faded when behind other objects.
    /// </param>
    public void DrawLocalCube(Matrix4x4 space, Vector3 size, Vector3 center = default(Vector3), float duration = 0, bool depthTest = true)
    {
        DrawLocalCube(space, size, Color.white, center, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a circle.
    /// </summary>
    /// <param name='position'>
    /// 	- Where the center of the circle will be positioned.
    /// </param>
    /// <param name='up'>
    /// 	- The direction perpendicular to the surface of the circle.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the circle.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the circle.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the circle.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the circle should be faded when behind other objects.
    /// </param>
    public void DrawCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f, float duration = 0, bool depthTest = true)
    {
        Vector3 _up = up.normalized * radius;
        Vector3 _forward = Vector3.Slerp(_up, -_up, 0.5f);
        Vector3 _right = Vector3.Cross(_up, _forward).normalized * radius;

        Matrix4x4 matrix = new Matrix4x4();

        matrix[0] = _right.x;
        matrix[1] = _right.y;
        matrix[2] = _right.z;

        matrix[4] = _up.x;
        matrix[5] = _up.y;
        matrix[6] = _up.z;

        matrix[8] = _forward.x;
        matrix[9] = _forward.y;
        matrix[10] = _forward.z;

        Vector3 _lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
        Vector3 _nextPoint = Vector3.zero;

        color = (color == default(Color)) ? Color.white : color;

        for (var i = 0; i < 91; i++)
        {
            _nextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad);
            _nextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad);
            _nextPoint.y = 0;

            _nextPoint = position + matrix.MultiplyPoint3x4(_nextPoint);

            DrawLine(_lastPoint, _nextPoint, color, duration, depthTest);
            _lastPoint = _nextPoint;
        }
    }

    /// <summary>
    /// 	- Debugs a circle.
    /// </summary>
    /// <param name='position'>
    /// 	- Where the center of the circle will be positioned.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the circle.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the circle.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the circle.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the circle should be faded when behind other objects.
    /// </param>
    public void DrawCircle(Vector3 position, Color color, float radius = 1.0f, float duration = 0, bool depthTest = true)
    {
        DrawCircle(position, Vector3.up, color, radius, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a circle.
    /// </summary>
    /// <param name='position'>
    /// 	- Where the center of the circle will be positioned.
    /// </param>
    /// <param name='up'>
    /// 	- The direction perpendicular to the surface of the circle.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the circle.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the circle.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the circle should be faded when behind other objects.
    /// </param>
    public void DrawCircle(Vector3 position, Vector3 up, float radius = 1.0f, float duration = 0, bool depthTest = true)
    {
        DrawCircle(position, up, Color.white, radius, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a circle.
    /// </summary>
    /// <param name='position'>
    /// 	- Where the center of the circle will be positioned.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the circle.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the circle.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the circle should be faded when behind other objects.
    /// </param>
    public void DrawCircle(Vector3 position, float radius = 1.0f, float duration = 0, bool depthTest = true)
    {
        DrawCircle(position, Vector3.up, Color.white, radius, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a wire sphere.
    /// </summary>
    /// <param name='position'>
    /// 	- The position of the center of the sphere.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the sphere.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the sphere.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the sphere.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the sphere should be faded when behind other objects.
    /// </param>
    public void DrawWireSphere(Vector3 position, Color color, float radius = 1.0f, float duration = 0, bool depthTest = true)
    {
        float angle = 10.0f;

        Vector3 x = new Vector3(position.x, position.y + radius * Mathf.Sin(0), position.z + radius * Mathf.Cos(0));
        Vector3 y = new Vector3(position.x + radius * Mathf.Cos(0), position.y, position.z + radius * Mathf.Sin(0));
        Vector3 z = new Vector3(position.x + radius * Mathf.Cos(0), position.y + radius * Mathf.Sin(0), position.z);

        Vector3 new_x;
        Vector3 new_y;
        Vector3 new_z;

        for (int i = 1; i < 37; i++)
        {

            new_x = new Vector3(position.x, position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad));
            new_y = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y, position.z + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad));
            new_z = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z);

            DrawLine(x, new_x, color, duration, depthTest);
            DrawLine(y, new_y, color, duration, depthTest);
            DrawLine(z, new_z, color, duration, depthTest);

            x = new_x;
            y = new_y;
            z = new_z;
        }
    }

    /// <summary>
    /// 	- Debugs a wire sphere.
    /// </summary>
    /// <param name='position'>
    /// 	- The position of the center of the sphere.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the sphere.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the sphere.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the sphere should be faded when behind other objects.
    /// </param>
    public void DrawWireSphere(Vector3 position, float radius = 1.0f, float duration = 0, bool depthTest = true)
    {
        DrawWireSphere(position, Color.white, radius, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a cylinder.
    /// </summary>
    /// <param name='start'>
    /// 	- The position of one end of the cylinder.
    /// </param>
    /// <param name='end'>
    /// 	- The position of the other end of the cylinder.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the cylinder.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the cylinder.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cylinder.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cylinder should be faded when behind other objects.
    /// </param>
    public void DrawCylinder(Vector3 start, Vector3 end, Color color, float radius = 1, float duration = 0, bool depthTest = true)
    {
        Vector3 up = (end - start).normalized * radius;
        Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
        Vector3 right = Vector3.Cross(up, forward).normalized * radius;

        //Radial circles
        DrawCircle(start, up, color, radius, duration, depthTest);
        DrawCircle(end, -up, color, radius, duration, depthTest);
        DrawCircle((start + end) * 0.5f, up, color, radius, duration, depthTest);

        //Side lines
        DrawLine(start + right, end + right, color, duration, depthTest);
        DrawLine(start - right, end - right, color, duration, depthTest);

        DrawLine(start + forward, end + forward, color, duration, depthTest);
        DrawLine(start - forward, end - forward, color, duration, depthTest);

        //Start endcap
        DrawLine(start - right, start + right, color, duration, depthTest);
        DrawLine(start - forward, start + forward, color, duration, depthTest);

        //End endcap
        DrawLine(end - right, end + right, color, duration, depthTest);
        DrawLine(end - forward, end + forward, color, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a cylinder.
    /// </summary>
    /// <param name='start'>
    /// 	- The position of one end of the cylinder.
    /// </param>
    /// <param name='end'>
    /// 	- The position of the other end of the cylinder.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the cylinder.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cylinder.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cylinder should be faded when behind other objects.
    /// </param>
    public void DrawCylinder(Vector3 start, Vector3 end, float radius = 1, float duration = 0, bool depthTest = true)
    {
        DrawCylinder(start, end, Color.white, radius, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a cone.
    /// </summary>
    /// <param name='position'>
    /// 	- The position for the tip of the cone.
    /// </param>
    /// <param name='direction'>
    /// 	- The direction for the cone gets wider in.
    /// </param>
    /// <param name='angle'>
    /// 	- The angle of the cone.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the cone.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cone.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cone should be faded when behind other objects.
    /// </param>
    public void DrawCone(Vector3 position, Vector3 direction, Color color, float angle = 45, float duration = 0, bool depthTest = true)
    {
        float length = direction.magnitude;

        Vector3 _forward = direction;
        Vector3 _up = Vector3.Slerp(_forward, -_forward, 0.5f);
        Vector3 _right = Vector3.Cross(_forward, _up).normalized * length;

        direction = direction.normalized;

        Vector3 slerpedVector = Vector3.Slerp(_forward, _up, angle / 90.0f);

        float dist;
        var farPlane = new Plane(-direction, position + _forward);
        var distRay = new Ray(position, slerpedVector);

        farPlane.Raycast(distRay, out dist);

        DrawRay(position, slerpedVector.normalized * dist, color);
        DrawRay(position, Vector3.Slerp(_forward, -_up, angle / 90.0f).normalized * dist, color, duration, depthTest);
        DrawRay(position, Vector3.Slerp(_forward, _right, angle / 90.0f).normalized * dist, color, duration, depthTest);
        DrawRay(position, Vector3.Slerp(_forward, -_right, angle / 90.0f).normalized * dist, color, duration, depthTest);

        DrawCircle(position + _forward, direction, color, (_forward - (slerpedVector.normalized * dist)).magnitude, duration, depthTest);
        DrawCircle(position + (_forward * 0.5f), direction, color, ((_forward * 0.5f) - (slerpedVector.normalized * (dist * 0.5f))).magnitude, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a cone.
    /// </summary>
    /// <param name='position'>
    /// 	- The position for the tip of the cone.
    /// </param>
    /// <param name='direction'>
    /// 	- The direction for the cone gets wider in.
    /// </param>
    /// <param name='angle'>
    /// 	- The angle of the cone.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cone.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cone should be faded when behind other objects.
    /// </param>
    public void DrawCone(Vector3 position, Vector3 direction, float angle = 45, float duration = 0, bool depthTest = true)
    {
        DrawCone(position, direction, Color.white, angle, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a cone.
    /// </summary>
    /// <param name='position'>
    /// 	- The position for the tip of the cone.
    /// </param>
    /// <param name='angle'>
    /// 	- The angle of the cone.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the cone.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cone.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cone should be faded when behind other objects.
    /// </param>
    public void DrawCone(Vector3 position, Color color, float angle = 45, float duration = 0, bool depthTest = true)
    {
        DrawCone(position, Vector3.up, color, angle, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a cone.
    /// </summary>
    /// <param name='position'>
    /// 	- The position for the tip of the cone.
    /// </param>
    /// <param name='angle'>
    /// 	- The angle of the cone.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the cone.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the cone should be faded when behind other objects.
    /// </param>
    public void DrawCone(Vector3 position, float angle = 45, float duration = 0, bool depthTest = true)
    {
        DrawCone(position, Vector3.up, Color.white, angle, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs an arrow.
    /// </summary>
    /// <param name='position'>
    /// 	- The start position of the arrow.
    /// </param>
    /// <param name='direction'>
    /// 	- The direction the arrow will point in.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the arrow.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the arrow.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the arrow should be faded when behind other objects. 
    /// </param>
    public void DrawArrow(Vector3 position, Vector3 direction, Color color, float duration = 0, bool depthTest = true)
    {
        DrawRay(position, direction, color, duration, depthTest);
        DrawCone(position + direction, -direction * 0.333f, color, 15, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs an arrow.
    /// </summary>
    /// <param name='position'>
    /// 	- The start position of the arrow.
    /// </param>
    /// <param name='direction'>
    /// 	- The direction the arrow will point in.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the arrow.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the arrow should be faded when behind other objects. 
    /// </param>
    public void DrawArrow(Vector3 position, Vector3 direction, float duration = 0, bool depthTest = true)
    {
        DrawArrow(position, direction, Color.white, duration, depthTest);
    }

    /// <summary>
    /// 	- Debugs a capsule.
    /// </summary>
    /// <param name='start'>
    /// 	- The position of one end of the capsule.
    /// </param>
    /// <param name='end'>
    /// 	- The position of the other end of the capsule.
    /// </param>
    /// <param name='color'>
    /// 	- The color of the capsule.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the capsule.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the capsule.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the capsule should be faded when behind other objects.
    /// </param>
    public void DrawCapsule(Vector3 start, Vector3 end, Color color, float radius = 1, float duration = 0, bool depthTest = true)
    {
        Vector3 up = (end - start).normalized * radius;
        Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
        Vector3 right = Vector3.Cross(up, forward).normalized * radius;

        float height = (start - end).magnitude;
        float sideLength = Mathf.Max(0, (height * 0.5f) - radius);
        Vector3 middle = (end + start) * 0.5f;

        start = middle + ((start - middle).normalized * sideLength);
        end = middle + ((end - middle).normalized * sideLength);

        //Radial circles
        DrawCircle(start, up, color, radius, duration, depthTest);
        DrawCircle(end, -up, color, radius, duration, depthTest);

        //Side lines
        DrawLine(start + right, end + right, color, duration, depthTest);
        DrawLine(start - right, end - right, color, duration, depthTest);

        DrawLine(start + forward, end + forward, color, duration, depthTest);
        DrawLine(start - forward, end - forward, color, duration, depthTest);

        for (int i = 1; i < 26; i++)
        {

            //Start endcap
            DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + start, Vector3.Slerp(right, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
            DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + start, Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
            DrawLine(Vector3.Slerp(forward, -up, i / 25.0f) + start, Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
            DrawLine(Vector3.Slerp(-forward, -up, i / 25.0f) + start, Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);

            //End endcap
            DrawLine(Vector3.Slerp(right, up, i / 25.0f) + end, Vector3.Slerp(right, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
            DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + end, Vector3.Slerp(-right, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
            DrawLine(Vector3.Slerp(forward, up, i / 25.0f) + end, Vector3.Slerp(forward, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
            DrawLine(Vector3.Slerp(-forward, up, i / 25.0f) + end, Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
        }
    }

    /// <summary>
    /// 	- Debugs a capsule.
    /// </summary>
    /// <param name='start'>
    /// 	- The position of one end of the capsule.
    /// </param>
    /// <param name='end'>
    /// 	- The position of the other end of the capsule.
    /// </param>
    /// <param name='radius'>
    /// 	- The radius of the capsule.
    /// </param>
    /// <param name='duration'>
    /// 	- How long to draw the capsule.
    /// </param>
    /// <param name='depthTest'>
    /// 	- Whether or not the capsule should be faded when behind other objects.
    /// </param>
    public void DrawCapsule(Vector3 start, Vector3 end, float radius = 1, float duration = 0, bool depthTest = true)
    {
        DrawCapsule(start, end, Color.white, radius, duration, depthTest);
    }
    #endregion
}
