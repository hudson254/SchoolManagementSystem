import { describe, it, expect } from 'vitest';
import { asList } from './listShape';

describe('asList (Assessment list-shape normalization)', () => {
  it('passes a bare array through unchanged', () => {
    const input = [{ id: 'u1', code: 'CS101' }, { id: 'u2', code: 'CS102' }];
    expect(asList(input)).toEqual(input);
  });

  it('unwraps a paginated envelope ({ items: [...] })', () => {
    const input = { items: [{ id: 'u1' }, { id: 'u2' }], total: 2, page: 1 };
    expect(asList(input)).toEqual([{ id: 'u1' }, { id: 'u2' }]);
  });

  it('returns an empty array for null/undefined (never crashes .map)', () => {
    expect(asList(null)).toEqual([]);
    expect(asList(undefined)).toEqual([]);
  });

  it('returns an empty array for malformed shapes (objects, scalars)', () => {
    expect(asList({ foo: 'bar' })).toEqual([]);
    expect(asList(42)).toEqual([]);
    expect(asList('nope')).toEqual([]);
  });

  it('handles an envelope with an empty items collection', () => {
    expect(asList({ items: [], total: 0 })).toEqual([]);
  });
});