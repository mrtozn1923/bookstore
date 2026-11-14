// watchPosition: one callback per new position until clearWatch()
let watchId = null;

document.getElementById("watch").addEventListener("click", () => {
  watchId = navigator.geolocation.watchPosition(
    ({ coords }) =>
      console.log(`position ${coords.latitude.toFixed(4)}, ${coords.longitude.toFixed(4)} (accuracy ${coords.accuracy} m)`),
    (err) => console.log(`watch error ${err.code}: ${err.message}`),
    { enableHighAccuracy: true, maximumAge: 0 },
  );
});

document.getElementById("stop").addEventListener("click", () => {
  navigator.geolocation.clearWatch(watchId);
  console.log("watch stopped");
});
