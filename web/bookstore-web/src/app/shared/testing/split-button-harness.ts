import { ComponentHarness } from '@angular/cdk/testing';

// Tests talk to the split button through this API, not through its markup or CSS classes
export class SplitButtonHarness extends ComponentHarness {
  static hostSelector = 'app-split-button';

  private readonly main = this.locatorFor('button.main');
  private readonly toggle = this.locatorFor('button.toggle');
  // The menu is in an overlay, outside the host element
  private readonly menuItems = this.documentRootLocatorFactory().locatorForAll('.split-menu-item');

  async getPrimaryText(): Promise<string> {
    return (await this.main()).text();
  }

  async isDisabled(): Promise<boolean> {
    return (await this.main()).getProperty<boolean>('disabled');
  }

  async clickPrimary(): Promise<void> {
    await (await this.main()).click();
  }

  async openMenu(): Promise<void> {
    const toggle = await this.toggle();
    if ((await toggle.getAttribute('aria-expanded')) !== 'true') {
      await toggle.click();
    }
  }

  async getMenuItems(): Promise<string[]> {
    await this.openMenu();
    return Promise.all((await this.menuItems()).map(item => item.text()));
  }

  async chooseFromMenu(text: string): Promise<void> {
    await this.openMenu();
    for (const item of await this.menuItems()) {
      if ((await item.text()) === text) {
        return item.click();
      }
    }
    throw new Error(`No menu item "${text}"`);
  }
}
