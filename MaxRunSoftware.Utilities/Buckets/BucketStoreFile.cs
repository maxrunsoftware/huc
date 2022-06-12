// Copyright (c) 2022 Max Run Software (dev@maxrunsoftware.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace MaxRunSoftware.Utilities;

public class BucketStoreFile : BucketStoreBase<string, string>
{
    public StringComparer Comparer;

    public BucketStoreFile(string file, StringComparison comparison = StringComparison.OrdinalIgnoreCase,
        string bucketNameDelimiter = ".")
    {
        File = Path.GetFullPath(file);
        Comparison = comparison;
        Comparer = Constant.StringComparison_StringComparer[comparison];
        BucketNameDelimiter = bucketNameDelimiter.CheckNotNull(nameof(bucketNameDelimiter));
    }

    public string File { get; }
    public StringComparison Comparison { get; }
    public string BucketNameDelimiter { get; }

    public IEnumerable<string> BucketNames => ReadFile().Keys;

    protected IDictionary<string, IDictionary<string, string>> ReadFile()
    {
        var props = new Dictionary<string, string>(Comparer);
        var jp = new JavaProperties();

        try
        {
            using (MutexLock.Create(TimeSpan.FromSeconds(10), File))
            {
                using (var fs = Util.FileOpenRead(File))
                {
                    jp.Load(fs, Constant.ENCODING_UTF8);
                }
            }
        }
        catch (MutexLockTimeoutException mte)
        {
            throw new IOException("Could not access file " + File, mte);
        }

        var jpd = jp.ToDictionary();
        foreach (var kl in jpd) props[kl.Key.TrimOrNull()] = kl.Value.TrimOrNull().WhereNotNull().LastOrDefault();

        var d = new Dictionary<string, IDictionary<string, string>>(Comparer);
        var bucketKeySplit = new[] { BucketNameDelimiter };
        foreach (var kvp in props)
        {
            var key = kvp.Key;
            var bucketValue = kvp.Value;
            if (key == null || bucketValue == null) continue;
            var keyParts = key.Split(bucketKeySplit, 2, StringSplitOptions.None).TrimOrNull().WhereNotNull().ToArray();
            if (keyParts.Length != 2) continue;
            var bucketName = keyParts.GetAtIndexOrDefault(0).TrimOrNull();
            var bucketKey = keyParts.GetAtIndexOrDefault(1).TrimOrNull();
            if (bucketName == null || bucketKey == null) continue;

            if (!d.TryGetValue(bucketName, out var dd))
            {
                dd = new Dictionary<string, string>(Comparer);
                d.Add(bucketName, dd);
            }

            dd[bucketKey] = bucketValue;
        }

        return d;
    }

    protected override string GetValue(string bucketName, string bucketKey)
    {
        bucketName = bucketName.CheckNotNullTrimmed(nameof(bucketName));
        bucketKey = bucketKey.CheckNotNullTrimmed(nameof(bucketKey));

        var d = ReadFile();
        if (d.TryGetValue(bucketName, out var dd))
            if (dd.TryGetValue(bucketKey, out var v))
                return v;

        return null;
    }

    protected override IEnumerable<string> GetKeys(string bucketName)
    {
        bucketName = bucketName.CheckNotNullTrimmed(nameof(bucketName));
        var d = ReadFile();
        if (d.TryGetValue(bucketName, out var dd)) return dd.Keys;
        return null;
    }

    protected override string CleanKey(string key)
    {
        return base.CleanKey(key.TrimOrNull());
    }

    protected override string CleanName(string name)
    {
        return base.CleanName(name.TrimOrNull());
    }

    protected override string CleanValue(string value)
    {
        return base.CleanValue(value.TrimOrNull());
    }

    protected override void SetValue(string bucketName, string bucketKey, string bucketValue)
    {
        bucketName = bucketName.CheckNotNullTrimmed(nameof(bucketName));
        bucketKey = bucketKey.CheckNotNullTrimmed(nameof(bucketKey));

        var bucketNameKey = bucketName + BucketNameDelimiter + bucketKey;

        var props = new Dictionary<string, string>(Comparer);
        var jp = new JavaProperties();
        try
        {
            using (MutexLock.Create(TimeSpan.FromSeconds(10), File))
            {
                using (var fs = Util.FileOpenRead(File))
                {
                    jp.Load(fs, Constant.ENCODING_UTF8);
                }
            }
        }
        catch (MutexLockTimeoutException mte)
        {
            throw new IOException("Could not access file " + File, mte);
        }

        string jpKey = null;
        string jpVal = null;
        foreach (var jpPropertyName in jp.GetPropertyNames())
        {
            var pn = jpPropertyName.TrimOrNull();
            if (pn == null) continue;
            if (string.Equals(pn, bucketNameKey, Comparison))
            {
                jpKey = jpPropertyName;
                jpVal = jp.GetProperty(jpKey);
                break;
            }
        }

        if (jpKey == null) // new key
        {
            if (bucketValue == null) return;
            jp.SetProperty(bucketNameKey, bucketValue);
        }
        else
        {
            if (string.Equals(bucketValue, jpVal.TrimOrNull(), Comparison)) return;
            if (bucketValue == null)
                jp.Remove(jpKey);
            else
                jp.SetProperty(bucketNameKey, bucketValue);
        }

        try
        {
            using (MutexLock.Create(TimeSpan.FromSeconds(10), File))
            {
                if (System.IO.File.Exists(File)) System.IO.File.Delete(File);
                using (var fs = Util.FileOpenWrite(File))
                {
                    jp.Store(fs, null, Constant.ENCODING_UTF8);
                    fs.FlushSafe();
                    fs.CloseSafe();
                }
            }
        }
        catch (MutexLockTimeoutException mte)
        {
            throw new IOException("Could not access file " + File, mte);
        }
    }
}