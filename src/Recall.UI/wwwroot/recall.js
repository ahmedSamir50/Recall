window.recallLatticeHandle = null;

window.recallSetDocumentCulture = (lang, dir) => {
  document.documentElement.lang = lang;
  document.documentElement.dir = dir;
};

window.recallApplyChrome = (theme, lang, dir) => {
  document.documentElement.dataset.theme = theme;
  document.documentElement.lang = lang;
  document.documentElement.dir = dir;
  window.recallSetLatticeTheme?.(theme);
};

window.recallMountAppLattice = (theme) => {
  const canvas = document.getElementById("bg");
  if (!canvas || !window.recallMountLattice) {
    return;
  }

  window.recallLatticeHandle?.destroy?.();
  window.recallLatticeHandle = window.recallMountLattice(canvas, {
    theme: theme || "dark",
    fps: 30,
    maxDpr: 1.25,
  });
};

window.recallMountLandingLattice = (theme) => {
  const canvas = document.getElementById("bg");
  if (!canvas || !window.recallMountLatticeClassic) {
    return;
  }

  window.recallLatticeHandle?.destroy?.();
  window.recallLatticeHandle = window.recallMountLatticeClassic(canvas, {
    theme: theme || "dark",
    fps: 30,
    maxDpr: 1.25,
  });
};

window.recallSetLatticeTheme = (theme) => {
  window.recallLatticeHandle?.setTheme?.(theme || "dark");
};

window.recallBoot = () => {
  recallApplyChrome("dark", "en", "ltr");
};
