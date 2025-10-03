using Rage;
using Rage.Native;

namespace HeliView
{
    internal enum ScaleformType
    {
        heli_cam,
        breaking_news
    }

    internal class Scaleform
    {
        protected int _handle;

        public Scaleform(ScaleformType type)
        {
            _handle = NativeFunction.Natives.REQUEST_SCALEFORM_MOVIE<int>(type.ToString());
        }

        public void Draw()
        {
            NativeFunction.Natives.DRAW_SCALEFORM_MOVIE_FULLSCREEN(_handle, 255, 255, 255, 255);
        }
    }

    internal class HeliCam : Scaleform
    {
        public float heading;
        public float altitude;
        public float field_of_view;

        public HeliCam() : base(ScaleformType.heli_cam)
        {
            heading = 0f;
            altitude = 0f;
            field_of_view = 50f;
        }

        public float Heading
        {
            get { return heading; }
            set
            {
                heading = value;
                NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD(_handle, "SET_CAM_HEADING");
                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_FLOAT(value);
                NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
            }
        }

        public float Altitude
        {
            get { return altitude; }
            set
            {
                altitude = value;
                NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD(_handle, "SET_CAM_ALT");
                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_FLOAT(value);
                NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
            }
        }
        // TODO
        public float FieldOfView
        {
            get { return field_of_view; }
            set
            {
                field_of_view = value;
                NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD(_handle, "SET_CAM_FOV");
                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_FLOAT(value);
                NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
            }
        }
    }

    internal class BreakingNews : Scaleform
    {
        private string title;
        private string subtitle;

        public BreakingNews() : base(ScaleformType.breaking_news)
        {
            title = "";
            subtitle = "";
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD(_handle, "SET_TEXT");
                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_LITERAL_STRING(value);
                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_LITERAL_STRING(subtitle);
                NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
            }
        }

        public string Subtitle
        {
            get { return subtitle; }
            set
            {
                subtitle = value;
                NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD(_handle, "SET_TEXT");
                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_LITERAL_STRING(title);
                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_LITERAL_STRING(value);
                NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
            }
        }
    }
}
