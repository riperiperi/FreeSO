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

        public UIJobInfo(bool maxLevel) : base(new UIAlertOptions()
        {
            Title = "Job Details",
            Width = 400,
            Height = maxLevel ? 200 : 348,
            Message = "",
            Buttons = new UIAlertButton[]
            {
                new UIAlertButton(UIAlertButtonType.OK, (btn) => { }),
            },
            AllowBB = true
        })
        {

            int standardVerticalSpace = 10;
            int verticalSpace = 40;

            OKButton = ButtonMap[UIAlertButtonType.OK];
            OKButton.OnButtonClick += (btn) => Destory();

            Type = new UILabel();
            Type.Position = new Vector2(30, verticalSpace);
            Type.Size = new Vector2(200, 16);
            Type.CaptionStyle = TextStyle.DefaultTitle;
            Type.CaptionStyle = Type.CaptionStyle.Clone();
            Type.CaptionStyle.Color = Color.White;
            Type.CaptionStyle.Size = 16;
            Type.CaptionStyle.Shadow = false;
            Type.Alignment = TextAlignment.Left | TextAlignment.Middle;
            this.Add(Type);
            verticalSpace += 16 + standardVerticalSpace;

            int sectionHeight = 54;

            UIImage HoursSectionImage = new UIImage(TextureGenerator.GenerateRoundedRectangle(GameFacade.GraphicsDevice, new Color(64, 101, 141), 360, sectionHeight, 6));
            HoursSectionImage.Position = new Vector2(20, verticalSpace);
            this.Add(HoursSectionImage);
            verticalSpace += standardVerticalSpace;

            int hoursTitleSectionHeight = (sectionHeight / 2) - 12 - 6;

            HoursTitle = new UILabel();
            HoursTitle.Position = new Vector2(40, verticalSpace + hoursTitleSectionHeight);
            HoursTitle.Size = new Vector2(200, 12);
            HoursTitle.CaptionStyle = TextStyle.DefaultTitle;
            HoursTitle.CaptionStyle = HoursTitle.CaptionStyle.Clone();
            HoursTitle.CaptionStyle.Color = Color.White;
            HoursTitle.CaptionStyle.Size = 14;
            HoursTitle.CaptionStyle.Shadow = false;
            HoursTitle.Alignment = TextAlignment.Left | TextAlignment.Middle;
            HoursTitle.Caption = "Hours";
            this.Add(HoursTitle);

            Hours = new UILabel();
            Hours.Position = new Vector2(40, verticalSpace);
            Hours.Size = new Vector2(320, 12);
            Hours.CaptionStyle = TextStyle.DefaultTitle;
            Hours.CaptionStyle = Hours.CaptionStyle.Clone();
            Hours.CaptionStyle.Color = Color.White;
            Hours.CaptionStyle.Size = 14;
            Hours.CaptionStyle.Shadow = false;
            Hours.Alignment = TextAlignment.Right | TextAlignment.Middle;
            this.Add(Hours);
            verticalSpace += 12 + standardVerticalSpace;

            CarPoolHours = new UILabel();
            CarPoolHours.Position = new Vector2(40, verticalSpace);
            CarPoolHours.Size = new Vector2(320, 12);
            CarPoolHours.CaptionStyle = TextStyle.DefaultTitle;
            CarPoolHours.CaptionStyle = CarPoolHours.CaptionStyle.Clone();
            CarPoolHours.CaptionStyle.Color = Color.White;
            CarPoolHours.CaptionStyle.Size = 14;
            CarPoolHours.CaptionStyle.Shadow = false;
            CarPoolHours.Alignment = TextAlignment.Right | TextAlignment.Middle;
            this.Add(CarPoolHours);
            verticalSpace += 12 + standardVerticalSpace + standardVerticalSpace;

            UIImage PositionSectionImage = new UIImage(TextureGenerator.GenerateRoundedRectangle(GameFacade.GraphicsDevice, new Color(64, 101, 141), 360, sectionHeight, 6));
            PositionSectionImage.Position = new Vector2(20, verticalSpace);
            this.Add(PositionSectionImage);
            verticalSpace += standardVerticalSpace;

            int positionTitleSectionHeight = (sectionHeight / 2) - 12 - 6;

            PositionTitle = new UILabel();
            PositionTitle.Position = new Vector2(40, verticalSpace + positionTitleSectionHeight);
            PositionTitle.Size = new Vector2(200, 12);
            PositionTitle.CaptionStyle = TextStyle.DefaultTitle;
            PositionTitle.CaptionStyle = PositionTitle.CaptionStyle.Clone();
            PositionTitle.CaptionStyle.Color = Color.White;
            PositionTitle.CaptionStyle.Size = 14;
            PositionTitle.CaptionStyle.Shadow = false;
            PositionTitle.Alignment = TextAlignment.Left | TextAlignment.Middle;
            PositionTitle.Caption = "Position";
            this.Add(PositionTitle);

            Title = new UILabel();
            Title.Position = new Vector2(40, verticalSpace);
            Title.Size = new Vector2(320, 12);
            Title.CaptionStyle = TextStyle.DefaultTitle;
            Title.CaptionStyle = Title.CaptionStyle.Clone();
            Title.CaptionStyle.Color = Color.White;
            Title.CaptionStyle.Size = 14;
            Title.CaptionStyle.Shadow = false;
            Title.Alignment = TextAlignment.Right | TextAlignment.Middle;
            this.Add(Title);
            verticalSpace += 12 + standardVerticalSpace;

            Level = new UILabel();
            Level.Position = new Vector2(40, verticalSpace);
            Level.Size = new Vector2(320, 12);
            Level.CaptionStyle = TextStyle.DefaultTitle;
            Level.CaptionStyle = Level.CaptionStyle.Clone();
            Level.CaptionStyle.Color = Color.White;
            Level.CaptionStyle.Size = 14;
            Level.CaptionStyle.Shadow = false;
            Level.Alignment = TextAlignment.Right | TextAlignment.Middle;
            this.Add(Level);
            verticalSpace += 12 + standardVerticalSpace + standardVerticalSpace;

            if (!maxLevel)
            {
                PromotionRequirements = new UILabel();
                PromotionRequirements.Position = new Vector2(30, verticalSpace);
                PromotionRequirements.Size = new Vector2(200, 16);
                PromotionRequirements.CaptionStyle = TextStyle.DefaultTitle;
                PromotionRequirements.CaptionStyle = PromotionRequirements.CaptionStyle.Clone();
                PromotionRequirements.CaptionStyle.Color = Color.White;
                PromotionRequirements.CaptionStyle.Size = 16;
                PromotionRequirements.CaptionStyle.Shadow = false;
                PromotionRequirements.Alignment = TextAlignment.Left | TextAlignment.Middle;
                PromotionRequirements.Caption = "Promotion Requirements:";
                this.Add(PromotionRequirements);
                verticalSpace += 16 + standardVerticalSpace;

                UIImage PerformanceSectionImage = new UIImage(TextureGenerator.GenerateRoundedRectangle(GameFacade.GraphicsDevice, new Color(64, 101, 141), 360, sectionHeight, 6));
                PerformanceSectionImage.Position = new Vector2(20, verticalSpace);
                this.Add(PerformanceSectionImage);

                int performanceTitleSectionHeight = (sectionHeight / 2) - 8;

                PerformanceTitle = new UILabel();
                PerformanceTitle.Position = new Vector2(40, verticalSpace + performanceTitleSectionHeight);
                PerformanceTitle.Size = new Vector2(200, 12);
                PerformanceTitle.CaptionStyle = TextStyle.DefaultTitle;
                PerformanceTitle.CaptionStyle = PerformanceTitle.CaptionStyle.Clone();
                PerformanceTitle.CaptionStyle.Color = Color.White;
                PerformanceTitle.CaptionStyle.Size = 14;
                PerformanceTitle.CaptionStyle.Shadow = false;
                PerformanceTitle.Alignment = TextAlignment.Left | TextAlignment.Middle;
                PerformanceTitle.Caption = "Performance";
                this.Add(PerformanceTitle);

                ProgressBar = new UIProgressBar()
                {
                    X = 360 - 180,
                    Y = verticalSpace + performanceTitleSectionHeight - 6,
                    Value = 75
                };
                ProgressBar.SetSize(185, 27);
                ProgressBar.Caption = "";
                ProgressBar.Background = TextureGenerator.GenerateRoundedRectangle(GameFacade.GraphicsDevice, new Color(58, 89, 122), 190, 27, 12);
                this.Add(ProgressBar);

                verticalSpace += sectionHeight + standardVerticalSpace;

                UIImage NextPositionSectionImage = new UIImage(TextureGenerator.GenerateRoundedRectangle(GameFacade.GraphicsDevice, new Color(64, 101, 141), 360, sectionHeight, 6));
                NextPositionSectionImage.Position = new Vector2(20, verticalSpace);
                this.Add(NextPositionSectionImage);

                int nextPositionTitleSectionHeight = (sectionHeight / 2) - 8;

                NextPositionTitle = new UILabel();
                NextPositionTitle.Position = new Vector2(40, verticalSpace + nextPositionTitleSectionHeight);
                NextPositionTitle.Size = new Vector2(200, 12);
                NextPositionTitle.CaptionStyle = TextStyle.DefaultTitle;
                NextPositionTitle.CaptionStyle = NextPositionTitle.CaptionStyle.Clone();
                NextPositionTitle.CaptionStyle.Color = Color.White;
                NextPositionTitle.CaptionStyle.Size = 14;
                NextPositionTitle.CaptionStyle.Shadow = false;
                NextPositionTitle.Alignment = TextAlignment.Left | TextAlignment.Middle;
                NextPositionTitle.Caption = "Next Position";
                this.Add(NextPositionTitle);

                NextPosition = new UILabel();
                NextPosition.Position = new Vector2(40, verticalSpace + nextPositionTitleSectionHeight);
                NextPosition.Size = new Vector2(320, 12);
                NextPosition.CaptionStyle = TextStyle.DefaultTitle;
                NextPosition.CaptionStyle = NextPosition.CaptionStyle.Clone();
                NextPosition.CaptionStyle.Color = Color.White;
                NextPosition.CaptionStyle.Size = 14;
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

        public void Show(JobInformation JobInfo)
        {
            Title.Caption = JobInfo.Title;
            Type.Caption = JobInfo.Type;
            Hours.Caption = JobInfo.Hours;
            Level.Caption = JobInfo.Level;
            CarPoolHours.Caption = JobInfo.CarpoolHours;
            if (!JobInfo.MaxLevel)
            {
                NextPosition.Caption = JobInfo.NextPosition;
                ProgressBar.Value = JobInfo.PromotionPercentage;
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
            GlobalShowDialog(new DialogReference
            {
                Dialog = dialog,
                Modal = modal
            });
        }

        public static void GlobalShowDialog(DialogReference dialog)
        {
            GameFacade.Screens.AddDialog(dialog);

            if (dialog.Dialog is UIDialog)
            {
                ((UIDialog)dialog.Dialog).CenterAround(UIScreen.Current, -(int)UIScreen.Current.X * 2, -(int)UIScreen.Current.Y * 2);
            }
        }
    }
}
