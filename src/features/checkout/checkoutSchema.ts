import * as z from 'zod';

// One schema: the form's types, its validation rules and its error messages
export const checkoutSchema = z.object({
  name: z.string().trim().min(1, { error: 'Enter your name' }).max(200),
  email: z.email({ error: 'Enter a valid email address' }),
  address: z.object({
    line1: z.string().trim().min(1, { error: 'Enter the street and number' }),
    city: z.string().trim().min(1, { error: 'Enter the city' }),
    postalCode: z.string().regex(/^\d{5}$/, { error: 'Postal codes have 5 digits' }),
  }),
  lines: z
    .array(z.object({
      isbn: z.string(),
      title: z.string(),
      unitPrice: z.number(),
      quantity: z.number({ error: 'Enter a number' }).int().min(1, { error: 'At least 1' }).max(99, { error: 'At most 99' }),
    }))
    .min(1, { error: 'Your order has no books' }),
});

export type CheckoutValues = z.infer<typeof checkoutSchema>;
