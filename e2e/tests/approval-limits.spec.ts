import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { InvoiceApprovalPage } from '../pages/invoice-approval.page';

test('Manager over-limit approval is rejected without changing the invoice', async ({ page }) => {
  const invoicePage = new InvoiceApprovalPage(page);
  const customerName = `E2E Approval Limit ${randomUUID()}`;

  await invoicePage.signIn('demo.manager', 'ChangeMe123!');
  const invoiceId = await invoicePage.createAndSubmitInvoice(customerName, 50000.01);
  const invoiceBeforeApproval = await invoicePage.getInvoice(invoiceId);

  await invoicePage.attemptApproval(invoiceId);

  await expect(page.getByRole('alert')).toHaveText(
    'Managers may approve invoices up to 50,000.'
  );
  const invoiceAfterApproval = await invoicePage.getInvoice(invoiceId);
  expect(invoiceAfterApproval).toEqual(invoiceBeforeApproval);
  await invoicePage.expectInvoicePending(invoiceId);
});
