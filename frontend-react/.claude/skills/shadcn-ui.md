---
name: shadcn/ui
description: shadcn/ui component library for React - button, card, dialog, and more. Run shadcn info --json to get project context.
trigger: "shadcn"
---

# shadcn/ui - AI Assistant Skill

## Overview

shadcn/ui is a component library built on **Radix UI primitives** + **Tailwind CSS** + **TypeScript**. Not a component package you install — you **copy and own the code**. Components live in your project.

Key principles:
- **Own the code** — components copied to `components/ui/`, fully customizable
- **Composition over configuration** — compose components freely
- **Accessible by default** — built on Radix UI primitives
- **Beautiful defaults** — designed to look great out of the box

## Project Detection

This skill activates when a `components.json` file exists in the project.

## Project Context

Run this command to get project configuration:
```
shadcn info --json
```
Returns: framework (next, vite, remix, etc.), Tailwind version, aliases, base library (radix or base), icon library, installed components, and resolved file paths.

## Framework Support

- **Next.js** (App Router and Pages Router)
- **Vite** ✅ — `pnpm dlx shadcn-vue@latest init` is for Vue; for React use `npx shadcn@latest init`
- **Remix**
- **Astro**
- **Laravel** (with Vite)

## Key CLI Commands

```bash
# Initialize shadcn in a project (creates components.json)
npx shadcn@latest init

# Add a component (copies source to components/ui/)
npx shadcn@latest add button
npx shadcn@latest add card dialog form

# Search components
npx shadcn@latest search

# View component docs
npx shadcn@latest docs add button

# Check for updates
npx shadcn@latest upgrade

# Diff (see what changed in a component)
npx shadcn@latest diff button
```

## Theming & Customization

### CSS Variables

shadcn uses CSS variables for theming in `app/globals.css`:

```css
--background: 0 0% 100%;
--foreground: 0 0% 3.9%;
--card: 0 0% 100%;
--card-foreground: 0 0% 3.9%;
--popover: 0 0% 100%;
--popover-foreground: 0 0% 3.9%;
--primary: 0 0% 9%;
--primary-foreground: 0 0% 98%;
--secondary: 0 0% 96.1%;
--secondary-foreground: 0 0% 9%;
--muted: 0 0% 96.1%;
--muted-foreground: 0 0% 45.1%;
--accent: 0 0% 96.1%;
--accent-foreground: 0 0% 9%;
--destructive: 0 84.2% 60.2%;
--destructive-foreground: 0 0% 98%;
--border: 0 0% 89.8%;
--input: 0 0% 89.8%;
--ring: 0 0% 3.9%;
--radius: 0.5rem;
```

### Theming Process

1. Edit CSS variables in `globals.css`
2. Or use `npx shadcn@latest init --yes` with preset colors

### Dark Mode

```bash
npx shadcn@latest init --dark
# or
npx shadcn@latest add dark-mode
```

Uses `next-themes` for Next.js or class-based approach with Tailwind `dark:` classes.

## Common Components

### Button
```tsx
import { Button } from "@/components/ui/button"

<Button variant="default">Click me</Button>
<Button variant="destructive">Delete</Button>
<Button variant="outline">Cancel</Button>
<Button variant="secondary">Save</Button>
<Button variant="ghost">Ghost</Button>
<Button variant="link">Link</Button>
<Button size="default">Default</Button>
<Button size="sm">Small</Button>
<Button size="lg">Large</Button>
<Button size="icon">�</Button>
```

### Card
```tsx
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"

<Card>
  <CardHeader>
    <CardTitle>Card Title</CardTitle>
    <CardDescription>Card description text</CardDescription>
  </CardHeader>
  <CardContent>
    <p>Card content</p>
  </CardContent>
  <CardFooter>
    <Button>Action</Button>
  </CardFooter>
</Card>
```

### Dialog (Modal)
```tsx
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"

<Dialog>
  <DialogTrigger asChild><Button>Open</Button></DialogTrigger>
  <DialogContent>
    <DialogHeader>
      <DialogTitle>Dialog Title</DialogTitle>
      <DialogDescription>Description</DialogDescription>
    </DialogHeader>
    <div>Content</div>
  </DialogContent>
</Dialog>
```

### Form (with React Hook Form + Zod)
```tsx
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"

const formSchema = z.object({
  username: z.string().min(2),
  email: z.string().email(),
})

const form = useForm({
  resolver: zodResolver(formSchema),
  defaultValues: { username: "", email: "" },
})

<Form {...form}>
  <form onSubmit={form.handleSubmit(onSubmit)}>
    <FormField control={form.control} name="username" render={({ field }) => (
      <FormItem>
        <FormLabel>Username</FormLabel>
        <FormControl><Input {...field} /></FormControl>
        <FormMessage />
      </FormItem>
    )} />
    <Button type="submit">Submit</Button>
  </form>
</Form>
```

### Data Table
```bash
npx shadcn@latest add table data-table
```
See docs for full pagination, sorting, filtering.

### Tabs
```tsx
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

<Tabs defaultValue="account">
  <TabsList>
    <TabsTrigger value="account">Account</TabsTrigger>
    <TabsTrigger value="password">Password</TabsTrigger>
  </TabsList>
  <TabsContent value="account">Account content</TabsContent>
  <TabsContent value="password">Password content</TabsContent>
</Tabs>
```

## components.json Configuration

```json
{
  "$schema": "https://ui.shadcn.com/schema.json",
  "style": "new-york",       // or "default"
  "rsc": false,              // React Server Components (true for Next.js App Router)
  "tsx": true,               // TypeScript (false for JavaScript)
  "tailwind": {
    "config": "tailwind.config.js",
    "css": "src/app/globals.css",
    "baseColor": "zinc",     // or "slate", "neutral", etc.
    "cssVariables": true
  },
  "iconLibrary": "lucide",   // or "lucide-react" (explicit)
  "aliases": {
    "components": "@/components",
    "utils": "@/lib/utils",
    "ui": "@/components/ui",
    "lib": "@/lib",
    "hooks": "@/hooks"
  }
}
```

## Import Aliases (jsconfig.json / tsconfig.json)

```json
{
  "compilerOptions": {
    "paths": {
      "@/*": ["./*"]
    }
  }
}
```

## MCP Server

The shadcn MCP server lets AI assistants browse, search, and install components from registries using natural language.

```bash
npx shadcn@latest mcp init --client claude
```

This adds to `.mcp.json`:
```json
{
  "mcpServers": {
    "shadcn": {
      "command": "npx",
      "args": ["shadcn@latest", "mcp"]
    }
  }
}
```

After restarting Claude Code, try:
- "Show me all available components in the shadcn registry"
- "Add the button, dialog and card components to my project"
- "Create a contact form using components from the shadcn registry"

## Best Practices

1. **Always run `shadcn info`** when working in a shadcn project to understand the project configuration
2. **Use `npx shadcn@latest add <component>`** to add components — don't copy-paste from docs
3. **Use composition** — shadcn components are designed to compose
4. **Use the CLI** — `shadcn search`, `shadcn docs`, `shadcn diff` are your friends
5. **Tailwind v4** uses `@theme` instead of `tailwind.config.js` for CSS variable customization
6. **Use Field component for forms** — it wraps Label + Input + Message automatically
7. **Use ToggleGroup for option sets** — not RadioGroup with individual Toggles

## Resources

- Docs: https://ui.shadcn.com/docs
- Components: https://ui.shadcn.com/docs/components
- CLI: https://ui.shadcn.com/docs/cli
- Theming: https://ui.shadcn.com/docs/theming
- Registry: https://ui.shadcn.com/docs/registry
- Skills: https://ui.shadcn.com/docs/skills
