// "Sonraki" link'i JavaScript varsa "daha fazla yükle" gibi çalışır; yoksa normal sayfa geçişidir.
// Crawler link'i izler (?page=n), kullanıcı aynı sayfada kalır ve adres çubuğu güncellenir.
document.addEventListener('click', async (event) => {
  const link = event.target.closest('a[data-load-more]');
  if (!link) return;
  event.preventDefault();
  const html = await (await fetch(link.href)).text();
  const next = new DOMParser().parseFromString(html, 'text/html');
  document.querySelector('main ul').append(...next.querySelectorAll('main ul > li'));
  document.querySelector('main nav').replaceWith(next.querySelector('main nav'));
  history.replaceState(null, '', link.href); // yenilenen ya da paylaşılan adres doğru sayfayı açar
});
