using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimplePaletteQuantizer.ColorCaches;
using SimplePaletteQuantizer.ColorCaches.Common;
using SimplePaletteQuantizer.ColorCaches.EuclideanDistance;
using SimplePaletteQuantizer.ColorCaches.LocalitySensitiveHash;
using SimplePaletteQuantizer.ColorCaches.Octree;
using SimplePaletteQuantizer.Ditherers;
using SimplePaletteQuantizer.Ditherers.ErrorDiffusion;
using SimplePaletteQuantizer.Ditherers.Ordered;
using SimplePaletteQuantizer.Helpers;
using SimplePaletteQuantizer.Properties;
using SimplePaletteQuantizer.Quantizers;
using SimplePaletteQuantizer.Quantizers.DistinctSelection;
using SimplePaletteQuantizer.Quantizers.MedianCut;
using SimplePaletteQuantizer.Quantizers.NeuQuant;
using SimplePaletteQuantizer.Quantizers.Octree;
using SimplePaletteQuantizer.Quantizers.OptimalPalette;
using SimplePaletteQuantizer.Quantizers.Popularity;
using SimplePaletteQuantizer.Quantizers.Uniform;
using SimplePaletteQuantizer.Quantizers.XiaolinWu;

namespace SimplePaletteQuantizer
{
    public partial class MainForm : Form
    {
        #region | Fields |

        private Image previewGifImage;
        private Image previewSourceImage;

        private Image sourceImage;
        private Image targetImage;

        private Boolean imageLoaded;
        private Boolean turnOnEvents;
        private Int32 projectedGifSize;
        private FileInfo sourceFileInfo;

        private ColorModel activeColorModel;
        private IColorCache activeColorCache;
        private IColorDitherer activeDitherer;
        private IColorQuantizer activeQuantizer;

        private List<ColorModel> colorModelList;
        private List<IColorCache> colorCacheList;
        private List<IColorDitherer> dithererList;
        private List<IColorQuantizer> quantizerList;
        private ConcurrentDictionary<Color, Int64> errorCache;

        #endregion

        #region | Constructors |

        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        #endregion

        #region | Update methods |

        private void UpdateImages()
        {
            // only perform if image was already loaded
            if (!imageLoaded) return;

            // prepares quantizer
            errorCache.Clear();

            // tries to retrieve an image based on HSB quantization
            Int32 parallelTaskCount = activeQuantizer.AllowParallel ? Convert.ToInt32(listParallel.Text) : 1;
            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Int32 colorCount = GetColorCount();

            // disables all the controls and starts running
            sourceImage = Image.FromFile(dialogOpenFile.FileName);
            Text = Resources.Running;
            SwitchControls(false);
            DateTime before = DateTime.Now;

            // quantization process
            Task quantization = Task.Factory.StartNew(() => 
                targetImage = ImageBuffer.QuantizeImage(sourceImage, activeQuantizer, activeDitherer, colorCount, parallelTaskCount), 
                TaskCreationOptions.LongRunning);

            // finishes after running
            quantization.ContinueWith(task =>
            {
                // detects operation duration
                TimeSpan duration = DateTime.Now - before;
                TimeSpan perPixel = new TimeSpan(duration.Ticks / (sourceImage.Width * sourceImage.Height));
                
                // detects error and color count
                Int32 originalColorCount = activeQuantizer.GetColorCount();
                String nrmsdString = string.Empty;

                // calculates NRMSD error, if requested
                if (checkShowError.Checked)
                {
                    Double nrmsd = ImageBuffer.CalculateImageNormalizedMeanError(sourceImage, targetImage, parallelTaskCount);
                    nrmsdString = string.Format(" (NRMSD = {0:0.#####})", nrmsd);
                }

                // spits some duration statistics (those actually slow the processing quite a bit, turn them off to make it quicker)
                editSourceInfo.Text = string.Format("Original: {0} colors ({1} x {2})", originalColorCount, sourceImage.Width, sourceImage.Height);
                editTargetInfo.Text = string.Format("Quantized: {0} colors{1}", colorCount, nrmsdString);

                // new GIF and PNG sizes
                Int32 newGifSize, newPngSize;

                // retrieves a GIF image based on our HSB-quantized one
                GetConvertedImage(targetImage, ImageFormat.Gif, out newGifSize);

                // retrieves a PNG image based on our HSB-quantized one
                GetConvertedImage(targetImage, ImageFormat.Png, out newPngSize);

                // spits out the statistics
                Text = string.Format("Simple palette quantizer (duration 0:{0:00}.{1:0000000}, per pixel 0.{2:0000000})", duration.Seconds, duration.Ticks, perPixel.Ticks);
                editProjectedGifSize.Text = projectedGifSize.ToString();
                editProjectedPngSize.Text = sourceFileInfo.Length.ToString();
                editNewGifSize.Text = newGifSize.ToString();
                editNewPngSize.Text = newPngSize.ToString();
                pictureTarget.Image = targetImage;

                // enables controls again
                SwitchControls(true);

            }, uiScheduler);
        }

        private void UpdateSourceImage()
        {
            switch (listSource.SelectedIndex)
            {
                case 0:
                    pictureSource.Image = previewSourceImage;
                    break;

                case 1:
                    pictureSource.Image = previewGifImage;
                    break;

                default:
                    throw new NotSupportedException("Not expected!");
            }
        }

        #endregion

        #region | Functions |

        private Int32 GetColorCount()
        {
            switch (listColors.SelectedIndex)
            {
                case 0: return 2;
                case 1: return 4;
                case 2: return 8;
                case 3: return 16;
                case 4: return 32;
                case 5: return 64;
                case 6: return 128;
                case 7: return 256;

                default:
                    throw new NotSupportedException("Only 2, 4, 8, 16, 32, 64, 128 and 256 colors are supported.");
            }
        }

        #endregion

        #region | Methods |

        private void ChangeQuantizer()
        {
            activeQuantizer = quantizerList[listMethod.SelectedIndex];

            // turns off the color option for the uniform quantizer, as it doesn't make sense
            listColors.Enabled = listMethod.SelectedIndex != 1 && listMethod.SelectedIndex != 6 && listMethod.SelectedIndex != 7;

            // enables the color cache option; where available
            listColorCache.Enabled = activeQuantizer is BaseColorCacheQuantizer;
            // listColorModel.Enabled = listColorCache.Enabled && turnOnEvents && activeColorCache is BaseColorCache && ((BaseColorCache)activeColorCache).IsColorModelSupported;

            // enables dithering when applicable
            listDitherer.Enabled = listMethod.SelectedIndex != 5;

            // enabled parallelism when supported
            listParallel.Enabled = activeQuantizer.AllowParallel;

            // applies current UI selection
            if (activeQuantizer is BaseColorCacheQuantizer)
            {
                BaseColorCacheQuantizer quantizer = (BaseColorCacheQuantizer) activeQuantizer;
                quantizer.ChangeCacheProvider(activeColorCache);
            }

            if (listMethod.SelectedIndex == 1 ||
                listMethod.SelectedIndex == 6 ||
                listMethod.SelectedIndex == 7)
            {
                turnOnEvents = false;
                listColors.SelectedIndex = 7;
                turnOnEvents = true;
            }
        }

        private void ChangeDitherer()
        {
            activeDitherer = dithererList[listDitherer.SelectedIndex];
        }

        private void ChangeColorCache()
        {
            activeColorCache = colorCacheList[listColorCache.SelectedIndex];

            // enables the color model option; where available
            // listColorModel.Enabled = turnOnEvents && activeColorCache is BaseColorCache && ((BaseColorCache)activeColorCache).IsColorModelSupported;

            // applies current UI selection
            if (activeQuantizer is BaseColorCacheQuantizer)
            {
                BaseColorCacheQuantizer quantizer = (BaseColorCacheQuantizer) activeQuantizer;
                quantizer.ChangeCacheProvider(activeColorCache);
            }

            // applies current UI selection
            if (activeColorCache is BaseColorCache)
            {
                BaseColorCache colorCache = (BaseColorCache)activeColorCache;
                colorCache.ChangeColorModel(activeColorModel);
            }
        }

        private void ChangeColorModel()
        {
            activeColorModel = colorModelList[listColorModel.SelectedIndex];

            // applies current UI selection
            if (activeColorCache is BaseColorCache)
            {
                BaseColorCache  colorCache = (BaseColorCache) activeColorCache;
                colorCache.ChangeColorModel(activeColorModel);
            }
        }

        private void EnableChoices()
        {
            Boolean allowColors = listMethod.SelectedIndex != 1 && listMethod.SelectedIndex != 6 && listMethod.SelectedIndex != 7;

            buttonUpdate.Enabled = true;
            checkShowError.Enabled = true;

            listSource.Enabled = true;
            listMethod.Enabled = true;

            listColorCache.Enabled = activeQuantizer is BaseColorCacheQuantizer;
            // listColorModel.Enabled = activeColorCache is BaseColorCache && allowColors;
            listColors.Enabled = allowColors;

            listDitherer.Enabled = listMethod.SelectedIndex != 5;
            listParallel.Enabled = activeQuantizer.AllowParallel;
        }

        private void SwitchControls(Boolean enabled)
        {
            // left panel
            panelSourceInfo.Enabled = enabled;
            panelFilename.Enabled = enabled;
            panelDirectory.Enabled = enabled;
            panelSource.Enabled = enabled;

            // right panel
            panelTargetInfo.Enabled = enabled;
            panelMethod.Enabled = enabled;
            panelColorCache.Enabled = enabled;
            panelDithering.Enabled = enabled;

            // bottom panel
            panelControls.Enabled = enabled;
        }

        private void GenerateProjectedGif()
        {
            // retrieves a projected GIF image (automatic C# conversion)
            Int32 projectedSize;
            previewGifImage = GetConvertedImage(sourceImage, ImageFormat.Gif, out projectedSize);
            projectedGifSize = projectedSize;
        }

        private static Image GetConvertedImage(Image image, ImageFormat newFormat, out Int32 imageSize)
        {
            Image result;

            // saves the image to the stream, and then reloads it as a new image format; thus conversion.. kind of
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, newFormat);
                stream.Seek(0, SeekOrigin.Begin);
                imageSize = (Int32)stream.Length;
                result = Image.FromStream(stream);
            }

            return result;
        }

        #endregion

        #region << Events >>

        private void MainFormLoad(object sender, EventArgs e)
        {
            errorCache = new ConcurrentDictionary<Color, Int64>();

            quantizerList = new List<IColorQuantizer>
            {
                new DistinctSelectionQuantizer(),
                new UniformQuantizer(),
                new PopularityQuantizer(),
                new MedianCutQuantizer(),
                new OctreeQuantizer(),
                new WuColorQuantizer(),
                new NeuralColorQuantizer(),
                new OptimalPaletteQuantizer()
            };

            dithererList = new List<IColorDitherer>
            {
                null,
                null,
                new BayerDitherer4(),
                new BayerDitherer8(),
                new ClusteredDotDitherer(),
                new DotHalfToneDitherer(),
                null,
                new FanDitherer(),
                new ShiauDitherer(),
                new SierraDitherer(),
                new StuckiDitherer(),
                new BurkesDitherer(),
                new AtkinsonDithering(),
                new TwoRowSierraDitherer(),
                new FloydSteinbergDitherer(),
                new JarvisJudiceNinkeDitherer()
            };

            colorCacheList = new List<IColorCache>
            {
                new EuclideanDistanceColorCache(),
                new LshColorCache(),
                new OctreeColorCache()
            };

            colorModelList = new List<ColorModel>
            {
                ColorModel.RedGreenBlue,
                ColorModel.LabColorSpace,
            };

            turnOnEvents = false;
            
            listSource.SelectedIndex = 0;
            listMethod.SelectedIndex = 0;
            listColors.SelectedIndex = 7;
            listColorCache.SelectedIndex = 0;
            listColorModel.SelectedIndex = 0;
            listDitherer.SelectedIndex = 0;
            listParallel.SelectedIndex = 3;

            ChangeQuantizer();
            ChangeColorCache();
            ChangeColorModel();

            turnOnEvents = true;
        }

        private void MainFormResize(object sender, EventArgs e)
        {
            panelRight.Width = panelMain.Width / 2;
        }

        private void ButtonBrowseClick(object sender, EventArgs e)
        {
            if (dialogOpenFile.ShowDialog() == DialogResult.OK)
            {
                editFilename.Text = Path.GetFileName(dialogOpenFile.FileName);
                editDirectory.Text = Path.GetDirectoryName(dialogOpenFile.FileName);
                previewSourceImage = Image.FromFile(dialogOpenFile.FileName);
                sourceFileInfo = new FileInfo(dialogOpenFile.FileName);
                sourceImage = Image.FromFile(dialogOpenFile.FileName);
                imageLoaded = true;

                GenerateProjectedGif();
                UpdateSourceImage();
                EnableChoices();
                UpdateImages();
            }
        }

        private void ListSourceSelectedIndexChanged(object sender, EventArgs e)
        {
            if (turnOnEvents) UpdateSourceImage();
        }

        private void ListMethodSelectedIndexChanged(object sender, EventArgs e)
        {
            if (turnOnEvents)
            {
                ChangeQuantizer();
                UpdateImages();
            }
        }

        private void ListDithererSelectedIndexChanged(object sender, EventArgs e)
        {
            if (turnOnEvents)
            {
                ChangeDitherer();
                UpdateImages();
            }
        }

        private void ListColorsSelectedIndexChanged(object sender, EventArgs e)
        {
            if (turnOnEvents) UpdateImages();
        }

        private void CheckShowErrorCheckedChanged(object sender, EventArgs e)
        {
            UpdateImages();
        }

        private void ListColorCacheSelectedIndexChanged(object sender, EventArgs e)
        {
            if (turnOnEvents)
            {
                ChangeColorCache();
                UpdateImages();
            }
        }

        private void ListColorModelSelectedIndexChanged(object sender, EventArgs e)
        {
            if (turnOnEvents)
            {
                ChangeColorModel();
                UpdateImages();
            }
        }

        private void ListParallelSelectedIndexChanged(object sender, EventArgs e)
        {
            if (turnOnEvents)
            {
                UpdateImages();
            }
        }

        private void ButtonUpdateClick(object sender, EventArgs e)
        {
            if (turnOnEvents)
            {
                UpdateImages();
            }
        }

        #endregion
    }
}
