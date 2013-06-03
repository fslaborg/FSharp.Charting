//----------------------------------------------------------------------------
// Copyright (c) 2002-2013 Microsoft Corporation and F# Open Source Group contributors.
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.html file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
//----------------------------------------------------------------------------

#r "System.Windows.Forms.DataVisualization.dll"
#I "../bin"
//#load "FSharp.Charting.fsx"
#r "../bin/FSharp.Charting.dll"

open FSharp.Charting
module FsiAutoShow = 
    fsi.AddPrinter(fun (ch:FSharp.Charting.ChartTypes.GenericChart) -> ch.ShowChart(); "(Chart)")





