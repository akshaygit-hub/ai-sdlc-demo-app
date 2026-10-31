import { expect, type Page } from '@playwright/test';

export class InvoiceApprovalPage {
  constructor(private readonly page: Page) {}

  async signIn(username: string, password: string): Promise<void> {
    await this.page.goto('/');
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
    await expect(this.page.getByText(`${username} (Manager)`)).toBeVisible();
  }

  async createAndSubmitInvoice(customerName: string, amount: number): Promise<string> {
    const createResponse = this.page.waitForResponse(response =>
      response.url().endsWith('/api/invoices') &&
      response.request().method() === 'POST');

    await this.page.getByLabel('Customer name').fill(customerName);
    await this.page.getByLabel('Amount').fill(String(amount));
    await this.page.getByRole('button', { name: 'Create invoice' }).click();

    const invoice = await (await createResponse).json() as { id: string };
    const submitResponse = this.page.waitForResponse(response =>
      response.url().endsWith(`/api/invoices/${invoice.id}/submit`) &&
      response.request().method() === 'POST');
    await this.page.getByRole('button', {
      name: `Submit invoice ${invoice.id} for approval`
    }).click();
    expect((await submitResponse).ok()).toBeTruthy();

    const invoiceRow = this.page.getByRole('row').filter({ hasText: invoice.id });
    await expect(invoiceRow).toContainText('Pending Approval');
    return invoice.id;
  }

  async getInvoice(invoiceId: string): Promise<unknown> {
    const token = await this.page.evaluate(() => {
      const session = sessionStorage.getItem('session');
      return session ? JSON.parse(session).token as string : null;
    });
    expect(token).toBeTruthy();

    const response = await this.page.request.get(`/api/invoices/${invoiceId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(response.ok()).toBeTruthy();
    return response.json();
  }

  async attemptApproval(invoiceId: string): Promise<void> {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
    await this.page.getByRole('button', { name: `Approve invoice ${invoiceId}` }).click();
  }

  async expectInvoicePending(invoiceId: string): Promise<void> {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
    const invoiceRow = this.page.getByRole('row').filter({ hasText: invoiceId });
    await expect(invoiceRow).toContainText('Pending Approval');
  }
}
