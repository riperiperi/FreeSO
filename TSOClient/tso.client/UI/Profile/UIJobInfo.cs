using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using FSO.Files;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Profile
{
    public struct JobInformation
    {
        public string Title;
        public string Type;
        public string Level;
        public string Hours;
        public string CarpoolHours;
        public string NextPosition;
        public int PromotionPercentage;
        public bool MaxLevel;

        public JobInformation(int jobGrade, int jobType, int jobExperience)
        {
            int poolTime = Math.Min(2, jobType - 1);
            int jobMultipler = -1;
            int jobExperienceFactor = 1;
            switch (jobType - 1)
            {
                case 0: // Robot Factory
                    jobMultipler = 5;
                    jobExperienceFactor = 2;
                    break;
                case 1: // Restaurant
                    jobMultipler = 5;
                    jobExperienceFactor = 2;
                    break;
                case 2: // Nightclub
                    jobMultipler = 10;
                    jobExperienceFactor = 1;
                    break;
                case 4: // Nightclub - Dancer
                    jobMultipler = 10;
                    jobExperienceFactor = 1;
                    break;
                default: // Other
                    jobMultipler = -1;
                    break;
            }

            float promotionPercentage = 0;
            if (jobMultipler >= 0 && jobGrade < 10)
            {
                int nextJobGrade = jobGrade + 1;
                float currentRequiredRounds = jobMultipler * (jobGrade * jobGrade); //jobMultipler * nextJobGrade^2
                float maxRequiredRounds = jobMultipler * (nextJobGrade * nextJobGrade); //jobMultipler * nextJobGrade^2
                float totalRoundsCompleted = jobExperience / jobExperienceFactor;
                float totalRoundsTillPromotion = maxRequiredRounds - totalRoundsCompleted;
                float currentRoundsTotal = maxRequiredRounds - currentRequiredRounds;
                promotionPercentage = (currentRoundsTotal - totalRoundsTillPromotion) / currentRoundsTotal;
            }

            Title = GameFacade.Strings.GetString("272", (((jobType - 1) * 11) + jobGrade + 1).ToString());
            Type = GameFacade.Strings.GetString("189", (67 + jobType).ToString());
            Level = jobGrade == 0 ? "Trainee" : "Level " + jobGrade;
            Hours = GameFacade.Strings.GetString("189", (73 + poolTime * 2).ToString());
            CarpoolHours = "Carpool at " + GameFacade.Strings.GetString("189", (74 + poolTime * 2).ToString());
            NextPosition = (jobGrade == 10) ? GameFacade.Strings.GetString("189", "79") :
                    GameFacade.Strings.GetString("272", (((jobType - 1) * 11) + jobGrade + 2).ToString());
            PromotionPercentage = (int)(promotionPercentage * 100);
            MaxLevel = (jobGrade == 10);
        }
    }

    public class UIJobInfo : UIAlert
    {
        private new UIButton OKButton;

        private UILabel Title;
        
        private UILabel Type;
        private UILabel Level;
        private UILabel Hours;
        private UILabel CarPoolHours;
        private UILabel NextPosition;
        private UIProgressBar ProgressBar;

        private UILabel PositionTitle;
        private UILabel HoursTitle;
        private UILabel PerformanceTitle;
        private UILabel NextPositionTitle;

        private UILabel PromotionRequirements;

        private JobInformation jobInformation;

        public UIJobInfo(JobInformation jobInformation) : base(new UIAlertOptions()
        {
            Title = "Job Details",
            Width = 400,
            Height = jobInformation.MaxLevel ? 200 : 348,
            Message = "",
            Buttons = new UIAlertButton[]
            {
                new UIAlertButton(UIAlertButtonType.OK, (btn) => { }),
            },
            AllowBB = true
        })
        {

            this.jobInformation = jobInformation;

            int standardVerticalSpace = 10;
            int verticalSpace = 40;

            OKButton = ButtonMap[UIAlertButtonType.OK];
            OKButton.OnButtonClick += (btn) => Destory();

            Type = new UILabel
            {
                Position = new Vector2(30, verticalSpace),
                Size = new Vector2(200, 16),
                CaptionStyle = TextStyle.DefaultTitle
            };
            Type.CaptionStyle = Type.CaptionStyle.Clone();
            Type.CaptionStyle.Color = Color.White;
            Type.CaptionStyle.Size = 16;
            Type.CaptionStyle.Shadow = true;
            Type.Alignment = TextAlignment.Left | TextAlignment.Middle;
            this.Add(Type);
            verticalSpace += 16 + standardVerticalSpace;

            int sectionHeight = 54;

            UIImage HoursSectionImage = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
            HoursSectionImage.SetSize(360, sectionHeight);
            HoursSectionImage.Position = new Vector2(20, verticalSpace);
            Add(HoursSectionImage);
            this.Add(HoursSectionImage);
            verticalSpace += standardVerticalSpace;

            int hoursTitleSectionHeight = (sectionHeight / 2) - 12 - 6;

            HoursTitle = new UILabel
            {
                Position = new Vector2(40, verticalSpace + hoursTitleSectionHeight),
                Size = new Vector2(200, 12),
                CaptionStyle = TextStyle.DefaultTitle
            };
            HoursTitle.CaptionStyle = HoursTitle.CaptionStyle.Clone();
            HoursTitle.CaptionStyle.Color = new Color(238, 247, 169);
            HoursTitle.CaptionStyle.Size = 14;
            HoursTitle.CaptionStyle.Shadow = false;
            HoursTitle.Alignment = TextAlignment.Left | TextAlignment.Middle;
            HoursTitle.Caption = "Hours";
            this.Add(HoursTitle);

            Hours = new UILabel
            {
                Position = new Vector2(40, verticalSpace),
                Size = new Vector2(320, 12),
                CaptionStyle = TextStyle.DefaultTitle
            };
            Hours.CaptionStyle = Hours.CaptionStyle.Clone();
            Hours.CaptionStyle.Color = new Color(238, 247, 169);
            Hours.CaptionStyle.Size = 12;
            Hours.CaptionStyle.Shadow = false;
            Hours.Alignment = TextAlignment.Right | TextAlignment.Middle;
            this.Add(Hours);
            verticalSpace += 12 + standardVerticalSpace;

            CarPoolHours = new UILabel
            {
                Position = new Vector2(40, verticalSpace),
                Size = new Vector2(320, 12),
                CaptionStyle = TextStyle.DefaultTitle
            };
            CarPoolHours.CaptionStyle = CarPoolHours.CaptionStyle.Clone();
            CarPoolHours.CaptionStyle.Color = new Color(238, 247, 169);
            CarPoolHours.CaptionStyle.Size = 12;
            CarPoolHours.CaptionStyle.Shadow = false;
            CarPoolHours.Alignment = TextAlignment.Right | TextAlignment.Middle;
            this.Add(CarPoolHours);
            verticalSpace += 12 + standardVerticalSpace + standardVerticalSpace;

            UIImage PositionSectionImage = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
            PositionSectionImage.SetSize(360, sectionHeight);
            PositionSectionImage.Position = new Vector2(20, verticalSpace);
            Add(PositionSectionImage);
            this.Add(PositionSectionImage);
            verticalSpace += standardVerticalSpace;

            int positionTitleSectionHeight = (sectionHeight / 2) - 12 - 6;

            PositionTitle = new UILabel
            {
                Position = new Vector2(40, verticalSpace + positionTitleSectionHeight),
                Size = new Vector2(200, 12),
                CaptionStyle = TextStyle.DefaultTitle
            };
            PositionTitle.CaptionStyle = PositionTitle.CaptionStyle.Clone();
            PositionTitle.CaptionStyle.Color = new Color(238, 247, 169);
            PositionTitle.CaptionStyle.Size = 14;
            PositionTitle.CaptionStyle.Shadow = false;
            PositionTitle.Alignment = TextAlignment.Left | TextAlignment.Middle;
            PositionTitle.Caption = "Position";
            this.Add(PositionTitle);

            Title = new UILabel
            {
                Position = new Vector2(40, verticalSpace),
                Size = new Vector2(320, 12),
                CaptionStyle = TextStyle.DefaultTitle
            };
            Title.CaptionStyle = Title.CaptionStyle.Clone();
            Title.CaptionStyle.Color = new Color(238, 247, 169);
            Title.CaptionStyle.Size = 12;
            Title.CaptionStyle.Shadow = false;
            Title.Alignment = TextAlignment.Right | TextAlignment.Middle;
            this.Add(Title);
            verticalSpace += 12 + standardVerticalSpace;

            Level = new UILabel
            {
                Position = new Vector2(40, verticalSpace),
                Size = new Vector2(320, 12),
                CaptionStyle = TextStyle.DefaultTitle
            };
            Level.CaptionStyle = Level.CaptionStyle.Clone();
            Level.CaptionStyle.Color = new Color(238, 247, 169);
            Level.CaptionStyle.Size = 12;
            Level.CaptionStyle.Shadow = false;
            Level.Alignment = TextAlignment.Right | TextAlignment.Middle;
            this.Add(Level);
            verticalSpace += 12 + standardVerticalSpace + standardVerticalSpace;

            if (!jobInformation.MaxLevel)
            {
                PromotionRequirements = new UILabel
                {
                    Position = new Vector2(30, verticalSpace),
                    Size = new Vector2(200, 16),
                    CaptionStyle = TextStyle.DefaultTitle
                };
                PromotionRequirements.CaptionStyle = PromotionRequirements.CaptionStyle.Clone();
                PromotionRequirements.CaptionStyle.Color = Color.White;
                PromotionRequirements.CaptionStyle.Size = 16;
                PromotionRequirements.CaptionStyle.Shadow = true;
                PromotionRequirements.Alignment = TextAlignment.Left | TextAlignment.Middle;
                PromotionRequirements.Caption = "Promotion Requirements:";
                this.Add(PromotionRequirements);
                verticalSpace += 16 + standardVerticalSpace;

                UIImage PerformanceSectionImage = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
                PerformanceSectionImage.SetSize(360, sectionHeight);
                PerformanceSectionImage.Position = new Vector2(20, verticalSpace);
                Add(PerformanceSectionImage);
                this.Add(PerformanceSectionImage);

                int performanceTitleSectionHeight = (sectionHeight / 2) - 8;

                PerformanceTitle = new UILabel
                {
                    Position = new Vector2(40, verticalSpace + performanceTitleSectionHeight),
                    Size = new Vector2(200, 12),
                    CaptionStyle = TextStyle.DefaultTitle
                };
                PerformanceTitle.CaptionStyle = PerformanceTitle.CaptionStyle.Clone();
                PerformanceTitle.CaptionStyle.Color = new Color(238, 247, 169);
                PerformanceTitle.CaptionStyle.Size = 14;
                PerformanceTitle.CaptionStyle.Shadow = false;
                PerformanceTitle.Alignment = TextAlignment.Left | TextAlignment.Middle;
                PerformanceTitle.Caption = "Performance";
                this.Add(PerformanceTitle);

                int progressBarWidth = 180;
                ProgressBar = new UIProgressBar()
                {
                    X = 360 - 180,
                    Y = verticalSpace + performanceTitleSectionHeight - 6,
                    Value = 75
                };
                ProgressBar.SetSize(progressBarWidth, 27);
                ProgressBar.Caption = "";
                ProgressBar.Background = TextureGenerator.GenerateRoundedRectangle(GameFacade.GraphicsDevice, new Color(64, 101, 141), progressBarWidth, 27, 12);
                this.Add(ProgressBar);

                verticalSpace += sectionHeight + standardVerticalSpace;

                UIImage NextPositionSectionImage = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
                NextPositionSectionImage.SetSize(360, sectionHeight);
                NextPositionSectionImage.Position = new Vector2(20, verticalSpace);
                Add(NextPositionSectionImage);
                this.Add(NextPositionSectionImage);

                int nextPositionTitleSectionHeight = (sectionHeight / 2) - 8;

                NextPositionTitle = new UILabel
                {
                    Position = new Vector2(40, verticalSpace + nextPositionTitleSectionHeight),
                    Size = new Vector2(200, 12),
                    CaptionStyle = TextStyle.DefaultTitle
                };
                NextPositionTitle.CaptionStyle = NextPositionTitle.CaptionStyle.Clone();
                NextPositionTitle.CaptionStyle.Color = new Color(238, 247, 169);
                NextPositionTitle.CaptionStyle.Size = 14;
                NextPositionTitle.CaptionStyle.Shadow = false;
                NextPositionTitle.Alignment = TextAlignment.Left | TextAlignment.Middle;
                NextPositionTitle.Caption = "Next Position";
                this.Add(NextPositionTitle);

                NextPosition = new UILabel
                {
                    Position = new Vector2(40, verticalSpace + nextPositionTitleSectionHeight),
                    Size = new Vector2(320, 12),
                    CaptionStyle = TextStyle.DefaultTitle
                };
                NextPosition.CaptionStyle = NextPosition.CaptionStyle.Clone();
                NextPosition.CaptionStyle.Color = new Color(238, 247, 169);
                NextPosition.CaptionStyle.Size = 12;
                NextPosition.CaptionStyle.Shadow = false;
                NextPosition.Alignment = TextAlignment.Right | TextAlignment.Middle;
                this.Add(NextPosition);
            }
        }

        public override void Removed()
        {
            base.Removed();
            Icon.Texture?.Dispose();
        }

        public void Show()
        {
            Title.Caption = jobInformation.Title;
            Type.Caption = jobInformation.Type;
            Hours.Caption = jobInformation.Hours;
            Level.Caption = jobInformation.Level;
            CarPoolHours.Caption = jobInformation.CarpoolHours;
            if (!jobInformation.MaxLevel)
            {
                NextPosition.Caption = jobInformation.NextPosition;
                ProgressBar.Value = jobInformation.PromotionPercentage;
            }
            ShowDialog(this, false);
            this.CenterAround(UIScreen.Current, -(int)UIScreen.Current.X * 2, -(int)UIScreen.Current.Y * 2);
        }

        public void Destory()
        {
            UIScreen.RemoveDialog(this);
        }

        public static void ShowDialog(UIElement dialog, bool modal)
        {
            ShowDialog(new DialogReference
            {
                Dialog = dialog,
                Modal = modal
            });
        }

        public static void ShowDialog(DialogReference dialog)
        {
            GameFacade.Screens.AddDialog(dialog);

            if (dialog.Dialog is UIDialog)
            {
                ((UIDialog)dialog.Dialog).CenterAround(UIScreen.Current, -(int)UIScreen.Current.X * 2, -(int)UIScreen.Current.Y * 2);
            }
        }
    }
}
