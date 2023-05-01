using System.IO;

namespace ImageResizer
{
    public class BitmapTag
    {

        public BitmapTag(object tag)
        {
            if (tag is string str) _path = str;
            if (tag is BitmapTag bTag)
            {
                _path = bTag.Path;
                _source = bTag.Source;
            }
        }

        public BitmapTag(string path, Stream source)
        {
            this._path = path;
            this._source = source;
        }
        private string _path = null;

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }
        private Stream _source = null;

        public Stream Source
        {
            get { return _source; }
            set { _source = value; }
        }

    }
}
