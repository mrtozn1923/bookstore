// The UMD build of Chart.js sets window.Chart when it is loaded.
import '../../lib/chart.js/dist/chart.umd.min.js';

// Passes the index of a clicked bar to .NET through the DotNetObjectReference.
export function onBarClick(chart, dotNetRef) {
  chart.options.onClick = (event, elements) => {
    if (elements.length > 0) {
      dotNetRef.invokeMethodAsync('SelectCategory', elements[0].index);
    }
  };
  chart.update();
}
