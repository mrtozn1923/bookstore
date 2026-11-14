// Prints the Baseline status of web-features ids: node scripts/baseline.mjs font-face text-box …
import { features } from 'web-features';

const label = s => s.baseline === 'high' ? `widely ${s.baseline_high_date}`
    : s.baseline === 'low' ? `newly ${s.baseline_low_date}` : 'not Baseline';
const browsers = { chrome: 'Chrome', firefox: 'Firefox', safari: 'Safari', safari_ios: 'iOS' };

for (const id of process.argv.slice(2)) {
    const status = features[id].status;
    const support = Object.entries(browsers)
        .map(([key, name]) => `${name} ${status.support[key] ?? '-'}`).join(', ');
    console.log(`${id.padEnd(26)} ${label(status).padEnd(18)} ${support}`);
}
