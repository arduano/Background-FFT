# "Background FFT" Realtime music visualizers
## About
The visualizers record system audio and display a visualizer in as real-time as possible (anywhere between 20ms and 100ms of latency, depending on the machine)

## Projects
**FancyVisualizer** The main project in this solution. It has a graphical OpenTK window with plenty of reactive audio features and two sensitivity sliders that can be opened by clicking the middle circle.

**Background FFT** An attempt at making the different projects modular.

**Background FFT Base** The backend of the visualizers. Records the audio and performs FFT, attempting to optimise the latency.

**BasicBarsWindow** The first attempt at making an OpenTK graphical window that shows the frequencies in a basic audio spectrum.

**KeyboardVisualizer** Making a visualizer for a variety of RGB devices using the RGB.NET sdk. Only tested on corsair products, but should theoretically also work on Razer, CoolerMaster, Novation, Logitech and Asus.

## Warning
If the RGB.NET sdk doesn't install itself automatically, nothing brakes if the projects is simply disabled in order to run another project.

Requires .NET 4.6.1+ to compile without errors.