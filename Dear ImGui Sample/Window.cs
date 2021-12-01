using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace Dear_ImGui_Sample
{
    public class Window : GameWindow
    {
        ImGuiController _controller;

        public Window() : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = new Vector2i(1600, 900), APIVersion = new Version(4, 5) })
        {
        }


        private Texture m_DirectoryIcon;
        private Texture m_FileIcon;

        protected override void OnLoad()
        {
            base.OnLoad();

            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);


            m_DirectoryIcon = new Texture("DirectoryIcon", new Bitmap("Resources/Icons/ContentBrowser/DirectoryIcon.png"), false, false);
            m_FileIcon      = new Texture("FileIcon", new Bitmap("Resources/Icons/ContentBrowser/FileIcon.png"), false, false);

            _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // Update the opengl viewport
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            // Tell ImGui of the new size
            _controller.WindowResized(ClientSize.X, ClientSize.Y);
        }


        private static string assetPath = Environment.CurrentDirectory + "\\" + "assets";
        private static string _currentDirectory = assetPath;

        float padding = 16.0f;
        float thumbnailSize = 128.0f;

        private string _draggingFilepath;

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            _controller.Update(this, (float)e.Time);

            GL.ClearColor(new Color4(0, 32, 48, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.None;
            ImGuiWindowFlags   window_flags    = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;

            ImGui.PushFont(_controller.arialFont);

            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowViewport(viewport.ID);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

            window_flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
            window_flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));
            ImGui.Begin("DockSpace Demo", window_flags);
            ImGui.PopStyleVar();
            ImGui.PopStyleVar(2);

            // DockSpace
            ImGuiIOPtr    io          = ImGui.GetIO();
            ImGuiStylePtr style       = ImGui.GetStyle();
            float         minWinSizeX = style.WindowMinSize.X;
            style.WindowMinSize.X = 370.0f;
            if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != ImGuiConfigFlags.None)
            {
                uint dockspace_id = ImGui.GetID("MyDockSpace");
                ImGui.DockSpace(dockspace_id, new Vector2(0.0f, 0.0f), dockspace_flags);
            }

            style.WindowMinSize.X = minWinSizeX;

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New", "Ctrl + N"))
                    {
                        Console.WriteLine("File -> New");
                    }
                }
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));

            if (ImGui.Begin("Content Browser", ImGuiWindowFlags.NoMove))
            {
                ImGui.PopStyleVar();
                ImGui.PopStyleVar(2);
                if (_currentDirectory != assetPath)
                {
                    if (ImGui.Button("<-"))
                    {
                        _currentDirectory = Directory.GetParent(_currentDirectory)!.FullName;
                    }
                }

                float cellSize = thumbnailSize + padding;

                float panelWidth  = ImGui.GetContentRegionAvail().X;
                int   columnCount = (int)(panelWidth / cellSize);
                if (columnCount < 1)
                    columnCount = 1;

                ImGui.Columns(columnCount, "0", false);

                var directoryInfo   = new DirectoryInfo(_currentDirectory);
                var fileSystemInfos = directoryInfo.GetFileSystemInfos();

                foreach (var entry in fileSystemInfos)
                {
                    var    fullPath     = entry.FullName;
                    var    relativePath = Path.GetRelativePath(assetPath, fullPath);
                    string filename     = Path.GetFileName(relativePath);

                    ImGui.PushID(filename);
                    Texture icon = entry.IsDirectory() ? m_DirectoryIcon : m_FileIcon;
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                    ImGui.ImageButton((IntPtr)icon.GLTexture, new Vector2(thumbnailSize, thumbnailSize), new Vector2(0, 1), new Vector2(1, 0));

                    if (ImGui.BeginDragDropSource())
                    {
                        _draggingFilepath = fullPath;

                        ImGui.SetDragDropPayload("file", IntPtr.Zero, 0);
                        ImGui.Text(filename);
                        ImGui.EndDragDropSource();
                    }

                    if (entry.IsDirectory() && ImGui.BeginDragDropTarget())
                    {
                        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            File.Move(_draggingFilepath, fullPath + "\\" + Path.GetFileName(_draggingFilepath));
                        }

                        ImGui.EndDragDropTarget();
                    }

                    ImGui.PopStyleColor();
                    if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        if (entry.IsDirectory())
                        {
                            _currentDirectory = fullPath;
                        }
                        else
                        {
                            Console.WriteLine($"File opening: {filename}");
                        }
                    }

                    ImGui.TextWrapped(filename);

                    ImGui.NextColumn();

                    ImGui.PopID();
                }

                ImGui.Columns(1);

                ImGui.SliderFloat("Thumbnail Size", ref thumbnailSize, 16, 512);
                ImGui.SliderFloat("Padding", ref padding, 0, 32);

                // TODO: status bar
                ImGui.End();
            }

            _controller.Render();

            Util.CheckGLError("End of frame");

            SwapBuffers();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            _controller.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _controller.MouseScroll(e.Offset);
        }
    }
}