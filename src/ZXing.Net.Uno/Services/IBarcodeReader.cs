﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Net.Uno.Readers;

public interface IBarcodeReader
{
	BarcodeReaderOptions Options { get; set; }

	BarcodeResult[] Decode(PixelBufferHolder image);
}
