using System;
using System.IO;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using Gigasoft.ProEssentials;
using Gigasoft.ProEssentials.Enums;

namespace DelaunayHeightmap
{
    /// <summary>
    /// ProEssentials WPF 3D Delaunay Heightmap — Point Cloud to 3D Surface
    ///
    /// Demonstrates 3D Delaunay triangulation using Pe3doWpf — the ProEssentials
    /// 3D scientific graph object. Loads the same 70-point acoustic survey data
    /// as example 147, but renders it as a 3D heightmap surface with contour
    /// coloring rather than a 2D contour fill.
    ///
    /// Delaunay3D triangulates the XZ plane and uses Y as height. The result is
    /// a 3D surface mesh where each measurement location becomes a vertex and
    /// the Y (dBA) value drives both height and contour color.
    ///
    /// Features:
    ///   - Loads DelaunaySample.txt — 70 space-delimited X Z Y measurement points
    ///     (note: column order X/Z/Y differs from example 147's X/Y/Z)
    ///   - PePlot.Option.Delaunay3D = true — triangulates the XZ plane from
    ///     scattered points, Y is the height / value axis
    ///   - ThreeDGraphPlottingMethod.Four — Surface with Contour
    ///   - ShowContour.BottomColors — 2D contour projected onto the floor
    ///   - Custom ContourColors array (6 stops: blue → cyan → green → yellow →
    ///     orange → red) matching example 147's color scale
    ///   - ManualContourScaleControl clamped to 80–102 dBA
    ///   - 3D graph annotations — SmallDotSolid + dBA text label at each point
    ///   - LightNoBorder QuickStyle (light theme)
    ///   - Direct3D render engine — required for Pe3do
    ///   - Mouse drag to rotate, mouse wheel to zoom, Shift+drag to pan
    ///   - DegreePrompting — shows rotation/zoom in subtitle during interaction
    ///
    /// Data model — Delaunay3D uses a flat, unstructured point list:
    ///   PeData.Subsets = 1       — always 1 subset for Delaunay3D
    ///   PeData.Points  = 70      — one entry per line in DelaunaySample.txt
    ///   PeData.X[0, p] = col 1   — Q/N (flow coefficient, horizontal X axis)
    ///   PeData.Z[0, p] = col 2   — PC  (depth axis, note column swap vs 147)
    ///   PeData.Y[0, p] = col 3   — dBA (height — the value being visualized)
    ///
    /// Controls:
    ///   Left-click drag           — rotate
    ///   Left-click drag + Shift   — pan / translate
    ///   Mouse wheel               — zoom in / out
    ///   Middle-button drag        — rotate light source
    ///   Double-click              — start / stop auto-rotation
    ///   Right-click               — context menu (export, print, customize)
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // -----------------------------------------------------------------------
        // Pe3do1_Loaded — chart initialization
        //
        // Always initialize ProEssentials in the control's Loaded event.
        // Do NOT initialize in the Window's Loaded event — the window fires
        // before the control is fully initialized.
        // -----------------------------------------------------------------------
        void Pe3do1_Loaded(object sender, RoutedEventArgs e)
        {
            // =======================================================================
            // Step 1 — Enable Delaunay3D and set plotting method
            //
            // Delaunay3D is a boolean toggle on the default surface mode — it is
            // NOT a separate PolyMode. Set it before the plotting method.
            //
            // ThreeDGraphPlottingMethod.Four = Surface with Contour — renders the
            // triangulated mesh as a solid colored surface with contour bands.
            // =======================================================================
            Pe3do1.PePlot.Option.Delaunay3D = true;
            Pe3do1.PePlot.Method = ThreeDGraphPlottingMethod.Four; // Surface With Contour

            // =======================================================================
            // Step 2 — Declare data dimensions
            //
            // Delaunay3D always uses a single subset (Subsets = 1).
            // Points is the total count of scattered XYZ measurement locations.
            // =======================================================================
            Pe3do1.PeData.Subsets = 1;
            Pe3do1.PeData.Points  = 70;

            // =======================================================================
            // Step 3 — Load DelaunaySample.txt
            //
            // Space-delimited file: col1  col2  col3 per line, 70 lines total.
            //
            // IMPORTANT — column mapping differs from example 147:
            //   col 1 → X  (Q/N — flow coefficient, horizontal X axis)
            //   col 2 → Z  (PC  — the depth axis in 3D space)
            //   col 3 → Y  (dBA — height, the quantity being visualized as elevation)
            //
            // In Pe3do, Y is always the vertical/height axis. Example 147 uses
            // Pesgo where Y is the vertical spatial axis. The swap here ensures
            // dBA drives the 3D height rather than being buried in the depth axis.
            //
            // Graph annotations are placed at each data point in 3D space:
            //   - SmallDotSolid marks the measurement location
            //   - Text label shows the Y (dBA) value at that point
            // =======================================================================
            int nPointCount = 0;

            string[] fileArray = { "", "" };
            try
            {
                fileArray = File.ReadAllLines("DelaunaySample.txt");
            }
            catch
            {
                MessageBox.Show(
                    "DelaunaySample.txt not found.\n\nMake sure DelaunaySample.txt is in the same folder as the executable.",
                    "File Not Found", MessageBoxButton.OK);
                Application.Current.Shutdown();
                return;
            }

            for (int i = 0; i < fileArray.Length; i++)
            {
                string line = fileArray[i];
                if (line.Length < 3) continue;

                var columns = line.Split(' ');
                float fX = float.Parse(columns[0], CultureInfo.InvariantCulture.NumberFormat);
                float fZ = float.Parse(columns[1], CultureInfo.InvariantCulture.NumberFormat); // col 2 → Z (note swap vs 147)
                float fY = float.Parse(columns[2], CultureInfo.InvariantCulture.NumberFormat); // col 3 → Y (dBA = height)

                // Chart data — XYZ positions for Delaunay3D triangulation
                Pe3do1.PeData.X[0, nPointCount] = fX;
                Pe3do1.PeData.Y[0, nPointCount] = fY;
                Pe3do1.PeData.Z[0, nPointCount] = fZ;

                // 3D graph annotation — dot + dBA label at each measurement location
                Pe3do1.PeAnnotation.Graph.X[nPointCount]     = fX;
                Pe3do1.PeAnnotation.Graph.Y[nPointCount]     = fY;
                Pe3do1.PeAnnotation.Graph.Z[nPointCount]     = fZ;
                Pe3do1.PeAnnotation.Graph.Type[nPointCount]  = (int)GraphAnnotationType.SmallDotSolid;
                Pe3do1.PeAnnotation.Graph.Color[nPointCount] = Color.FromArgb(255, 0, 0, 0);
                Pe3do1.PeAnnotation.Graph.Text[nPointCount]  = string.Format("{0:##0.0}", fY);

                nPointCount++;
            }

            // =======================================================================
            // Step 4 — Null data values and axis padding
            // =======================================================================
            Pe3do1.PeData.NullDataValueX = -9999;
            Pe3do1.PeData.NullDataValue  = -9999;
            Pe3do1.PeData.NullDataValueZ = -9999;

            Pe3do1.PeGrid.Configure.AutoPadBeyondZeroX = true;
            Pe3do1.PeConfigure.ImageAdjustLeft         = 100;

            // =======================================================================
            // Step 5 — Titles
            // =======================================================================
            Pe3do1.PeString.MainTitle         = "Hand Held Sound Meter Readings [dBA]";
            Pe3do1.PeString.SubTitle          = "CVHF 1300.31 Impeller Diameter: LTO 40214, DGI=6";
            Pe3do1.PeString.MultiSubTitles[0] = "|Contour of DRS Attribute = [1339]|";

            Pe3do1.PeFont.MainTitle.Font = "Arial";
            Pe3do1.PeFont.SubTitle.Font  = "Arial";

            Pe3do1.PeString.YAxisLabel = "PC";
            Pe3do1.PeString.XAxisLabel = "Q/N";

            // =======================================================================
            // Step 6 — Manual contour range
            //
            // ManualContourScaleControl on Pe3do clamps the 3D contour color scale
            // to the known dBA measurement range — prevents outliers from skewing
            // the entire color ramp.
            //
            // Note: Pe3do uses PePlot.Option.ManualContourScaleControl (not
            // PeGrid.Configure.ManualScaleControlZ as Pesgo does).
            // =======================================================================
            Pe3do1.PePlot.Option.ManualContourScaleControl = ManualScaleControl.MinMax;
            Pe3do1.PePlot.Option.ManualContourMin          = 80.0F;
            Pe3do1.PePlot.Option.ManualContourMax          = 102.0F;

            // =======================================================================
            // Step 7 — Custom contour color array
            //
            // 6 color stops define the gradient from low to high dBA.
            // ContourColorBlends = 4 — slightly more blending than example 147's 2,
            // appropriate for a 3D surface where smooth transitions read better.
            //
            // ContourColorBlends must always be set BEFORE ContourColorSet.
            // =======================================================================
            Pe3do1.PeColor.ContourColors.Clear(6);
            Pe3do1.PeColor.ContourColors[0] = Color.FromArgb(255, 0,   0,   255); // blue
            Pe3do1.PeColor.ContourColors[1] = Color.FromArgb(255, 17,  211, 214); // cyan
            Pe3do1.PeColor.ContourColors[2] = Color.FromArgb(255, 0,   255, 0);   // green
            Pe3do1.PeColor.ContourColors[3] = Color.FromArgb(255, 255, 255, 0);   // yellow
            Pe3do1.PeColor.ContourColors[4] = Color.FromArgb(255, 245, 181, 5);   // orange
            Pe3do1.PeColor.ContourColors[5] = Color.FromArgb(255, 255, 0,   0);   // red

            Pe3do1.PeColor.ContourColorBlends = 4;
            Pe3do1.PeColor.ContourColorAlpha  = 225;
            Pe3do1.PeColor.ContourColorSet    = ContourColorSet.ContourColors;

            // =======================================================================
            // Step 8 — Axis padding and legend
            // =======================================================================
            Pe3do1.PeGrid.Configure.AutoMinMaxPadding = 1;

            Pe3do1.PeLegend.Location                  = LegendLocation.Right;
            Pe3do1.PeLegend.Show                      = true;
            Pe3do1.PeUserInterface.Menu.LegendLocation = MenuControl.Show;
            Pe3do1.PeLegend.ContourStyle              = true;
            Pe3do1.PeLegend.ContourLegendPrecision    = ContourLegendPrecision.ZeroDecimals;

            // =======================================================================
            // Step 9 — Bottom contour projection
            //
            // ShowContour.BottomColors projects a 2D colored contour onto the floor
            // of the 3D scene — gives a plan-view reference while rotating the 3D
            // surface, matching the 2D view in example 147.
            // =======================================================================
            Pe3do1.PePlot.Option.ShowContour = ShowContour.BottomColors;

            // =======================================================================
            // Step 10 — Theme, fonts, and rendering engine
            //
            // RenderEngine.Direct3D must be set for Pe3do.
            // LightNoBorder QuickStyle — light theme (contrast to example 147's medium).
            // =======================================================================
            Pe3do1.PeConfigure.RenderEngine       = RenderEngine.Direct3D;
            Pe3do1.PeColor.BitmapGradientMode     = true;
            Pe3do1.PeColor.QuickStyle             = QuickStyle.LightNoBorder;

            Pe3do1.PeFont.Fixed                   = true;
            Pe3do1.PeFont.FontSize                = Gigasoft.ProEssentials.Enums.FontSize.Medium;
            Pe3do1.PeFont.SizeGlobalCntl          = 1.1F;
            Pe3do1.PeConfigure.TextShadows        = TextShadows.BoldText;
            Pe3do1.PeFont.Label.Bold              = true;

            Pe3do1.PeData.Precision               = DataPrecision.TwoDecimals;

            // =======================================================================
            // Step 11 — Surface colors
            //
            // SurfaceColors.WireFrame controls the color of the wireframe mesh lines.
            // SurfaceColors.SolidSurface controls the base surface fill color before
            // contour coloring is applied.
            // =======================================================================
            Pe3do1.PeColor.SubsetColors[(int)(SurfaceColors.WireFrame)]   = Color.FromArgb(255, 198, 0,   0);
            Pe3do1.PeColor.SubsetColors[(int)(SurfaceColors.SolidSurface)] = Color.FromArgb(255, 0,   148, 90);

            // =======================================================================
            // Step 12 — Image padding
            // =======================================================================
            Pe3do1.PeConfigure.ImageAdjustLeft   = 100;
            Pe3do1.PeConfigure.ImageAdjustRight  = 100;
            Pe3do1.PeConfigure.ImageAdjustBottom = 100;

            // =======================================================================
            // Step 13 — Performance and interaction setup
            // =======================================================================
            Pe3do1.PeConfigure.PrepareImages          = true;
            Pe3do1.PeConfigure.CacheBmp               = true;
            Pe3do1.PeUserInterface.Allow.FocalRect    = false;

            // =======================================================================
            // Step 14 — 3D camera and viewport
            //
            // DxZoom controls the camera distance (positive = closer, negative = farther).
            // ViewingHeight sets the vertical tilt of the camera (0–50 range).
            // DegreeOfRotation sets the initial horizontal rotation angle.
            // DxFitControlShape = false prevents auto-fitting to the window shape.
            // GridAspectY compresses the Y (height) scale for better visual proportion.
            // =======================================================================
            Pe3do1.PePlot.Option.DxFitControlShape = false;
            Pe3do1.PeGrid.Option.GridAspectY       = .5F;

            Pe3do1.PePlot.Option.DxZoom            = .3F;
            Pe3do1.PeUserInterface.Scrollbar.ViewingHeight    = 26;
            Pe3do1.PeUserInterface.Scrollbar.DegreeOfRotation = 180;

            Pe3do1.PePlot.Option.DxZoomMax              = 3F;
            Pe3do1.PePlot.Option.DxZoomMin              = -.20F;
            Pe3do1.PePlot.Option.DxViewportPanFactor    = 2.0F;
            Pe3do1.PeUserInterface.Scrollbar.MouseWheelZoomFactor = 3.75F;

            // =======================================================================
            // Step 15 — Lighting
            //
            // SetLight(index, x, y, z) positions the light source in 3D space.
            // LightStrength controls the intensity of the directional light.
            // =======================================================================
            Pe3do1.PeFunction.SetLight(0, -9.0F, -3.2F, 1.0F);
            Pe3do1.PePlot.Option.LightStrength = .35F;

            // =======================================================================
            // Step 16 — DegreePrompting
            //
            // Shows current rotation angle and zoom level in the subtitle area
            // during user interaction — useful for dialing in default view settings.
            // =======================================================================
            Pe3do1.PePlot.Option.DegreePrompting = true;

            // =======================================================================
            // Step 17 — Cursor and hotspot
            //
            // YValue prompt shows the dBA elevation value at the highlighted vertex.
            // HotSpot.Data = true enables vertex highlighting on hover.
            // HighlightColor sets the color of the highlighted data point.
            // =======================================================================
            Pe3do1.PeUserInterface.Cursor.PromptTracking      = true;
            Pe3do1.PeUserInterface.Cursor.PromptStyle         = CursorPromptStyle.YValue;
            Pe3do1.PeUserInterface.Cursor.TrackingTooltipTitle = "dBA";
            Pe3do1.PeUserInterface.Cursor.PromptLocation      = CursorPromptLocation.ToolTip;
            Pe3do1.PeUserInterface.Cursor.HighlightColor      = Color.FromArgb(255, 255, 0, 0);
            Pe3do1.PeUserInterface.HotSpot.Data               = true;

            // =======================================================================
            // Step 18 — Context menu configuration
            // =======================================================================
            Pe3do1.PeUserInterface.Menu.DataShadow              = MenuControl.Show;
            Pe3do1.PeUserInterface.Menu.ShowWireFrame           = MenuControl.Hide;
            Pe3do1.PeUserInterface.Menu.AnnotationControl       = true;
            Pe3do1.PeUserInterface.Menu.ShowAnnotationText      = MenuControl.Show;
            Pe3do1.PeUserInterface.Menu.AnnotationTextFixedSize = MenuControl.Show;

            // =======================================================================
            // Step 19 — Smooth rotation and drag
            // =======================================================================
            Pe3do1.PeUserInterface.Scrollbar.ScrollSmoothness        = 2;
            Pe3do1.PeUserInterface.Scrollbar.MouseWheelZoomSmoothness = 2;
            Pe3do1.PeUserInterface.Scrollbar.PinchZoomSmoothness      = 2;
            Pe3do1.PeUserInterface.Scrollbar.MouseDraggingX           = true;
            Pe3do1.PeUserInterface.Scrollbar.MouseDraggingY           = true;

            // =======================================================================
            // Step 20 — 3D graph annotation display settings
            //
            // LeftJustificationOutside = true — smart default orientation for labels,
            //   placing text outside the surface mesh for readability.
            // SymbolObstacles = true — labels try to avoid overlapping each other.
            // AnnotationTextFixedSize = true — text stays the same size regardless
            //   of zoom level (menu-controllable).
            // SizeCntl scales the overall annotation symbol size.
            // DxSphereComplexity controls polygon count for 3D sphere symbols.
            // =======================================================================
            Pe3do1.PeAnnotation.Graph.LeftJustificationOutside = true;
            Pe3do1.PeAnnotation.Graph.SymbolObstacles          = true;
            Pe3do1.PeFont.GraphAnnotationTextSize              = 90;
            Pe3do1.PeUserInterface.HotSpot.GraphAnnotation     = true;
            Pe3do1.PePlot.Option.DxSphereComplexity            = 12;
            Pe3do1.PeAnnotation.Graph.SizeCntl                 = 1.2f;
            Pe3do1.PeAnnotation.Graph.AnnotationTextFixedSize  = true;
            Pe3do1.PeAnnotation.Show                           = true;
            Pe3do1.PeAnnotation.Graph.Show                     = true;

            // =======================================================================
            // Step 21 — Initial camera ViewingAt point
            //
            // SetViewingAt centers the camera on the first data annotation point,
            // giving a sensible default view of the surface on first load.
            // =======================================================================
            float vx = (float)Pe3do1.PeAnnotation.Graph.X[0];
            float vy = (float)Pe3do1.PeAnnotation.Graph.Y[0];
            float vz = (float)Pe3do1.PeAnnotation.Graph.Z[0];
            Pe3do1.PeFunction.SetViewingAt(vx, vy, vz);

            // =======================================================================
            // Step 22 — Finalization (Pe3do-specific sequence)
            //
            // Force3dxVerticeRebuild rebuilds the GPU geometry buffers.
            // Force3dxAnnotVerticeRebuild rebuilds the 3D annotation geometry.
            // ReinitializeResetImage applies all properties and renders.
            // Invalidate + Refresh — Pe3do typically needs explicit Refresh().
            // =======================================================================
            Pe3do1.PeFunction.Force3dxVerticeRebuild       = true;
            Pe3do1.PeFunction.Force3dxAnnotVerticeRebuild  = true;

            Pe3do1.PeFunction.ReinitializeResetImage();
            Pe3do1.Invalidate();
            Pe3do1.Refresh();
        }

        // -----------------------------------------------------------------------
        // Window_Closing
        // -----------------------------------------------------------------------
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
