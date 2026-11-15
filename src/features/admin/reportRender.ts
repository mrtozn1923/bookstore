import type { ProfilerOnRenderCallback } from 'react';

// <Profiler> calls this in development and in the profiling build; a normal production build never does
export const reportRender: ProfilerOnRenderCallback = (id, phase, actualDuration, baseDuration) => {
  console.debug(`[profiler] ${id} ${phase} actual=${actualDuration.toFixed(1)}ms base=${baseDuration.toFixed(1)}ms`);
};
