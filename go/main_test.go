package bench

import (
	"log"
	"os"
	"testing"

	"github.com/bmatsuo/lmdb-go/lmdb"
	"github.com/pkg/errors"

	fb "github.com/google/flatbuffers/go"
)

var (
	db  lmdb.DBI
	env *lmdb.Env
)

func init() {

	var folder = "tmp"
	os.RemoveAll(folder)

	var err error
	env, err = lmdb.NewEnv()

	if err != nil {
		panic(errors.Wrap(err, "env create failed"))
	}

	err = env.SetMaxDBs(1)
	if err != nil {
		panic(err)
	}
	err = env.SetMapSize(1 << 20)
	if err != nil {
		panic("map size failed")
	}

	if err = env.SetFlags(lmdb.MapAsync); err != nil {
		panic(errors.Wrap(err, "set flag"))
	}

	os.MkdirAll(folder, os.ModePerm)
	err = env.Open(folder, lmdb.MapAsync, os.ModePerm)
	if err != nil {
		panic(errors.Wrap(err, "open env in "+folder))
	}

	var staleReaders int
	if staleReaders, err = env.ReaderCheck(); err != nil {
		panic(errors.Wrap(err, "reader check"))
	}
	if staleReaders > 0 {
		log.Printf("cleared %d reader slots from dead processes", staleReaders)
	}

	err = env.Update(func(txn *lmdb.Txn) (err error) {
		db, err = txn.CreateDBI("agg")
		return err
	})
	if err != nil {
		panic(errors.Wrap(err, "create DB"))
	}
}

const (
	BinItemCount    int    = 5
	RestockCount           = 20
	ProductIDOffset        = 100000
	BinCode                = "RandomBinHere"
	SaleID          uint64 = 1234023
	BinFlags               = BinFlagsHasSale
	WriteTx                = lmdb.NoSync
)

func CreateBin(key []byte) {

	tx, err := env.BeginTxn(nil, 0)
	if err != nil {
		panic(err)
	}

	buf := fb.NewBuilder(1024)
	BinStartItemsVector(buf, BinItemCount)

	for i := 0; i < BinItemCount; i++ {
		itemID := uint64(i + ProductIDOffset)
		CreateBinItem(buf, itemID, 0, uint32(i+1), ItemTypeProduct)
	}
	bins := buf.EndVector(BinItemCount)
	code := buf.CreateString(BinCode)

	BinStart(buf)
	BinAddItems(buf, bins)
	BinAddType(buf, ItemTypeBin)
	BinAddFlags(buf, BinFlags)
	BinAddCode(buf, code)
	BinAddSubtype(buf, 1)
	BinAddSaleID(buf, SaleID)
	bin := BinEnd(buf)
	buf.Finish(bin)

	tx.Put(db, key, buf.FinishedBytes(), 0)
	tx.Commit()

}

func BenchmarkFlatBuffersRead(b *testing.B) {

	var (
		data []byte

		item = &BinItem{}
		key  = []byte{1}
	)
	CreateBin(key)

	var counter uint64

	tx, err := env.BeginTxn(nil, lmdb.Readonly)
	if err != nil {
		panic(err)
	}

	tx.RawRead = true
	for i := 0; i < b.N; i++ {
		tx.Reset()
		tx.Renew()
		data, err = tx.Get(db, key)
		if err != nil {
			panic(err)
		}

		bin := GetRootAsBin(data, 0)
		search := uint64(i%BinItemCount + ProductIDOffset)
		for j := 0; j < bin.ItemsLength(); j++ {
			bin.Items(item, j)
			if item.ItemID() == search {
				counter += uint64(item.Count())
				break
			}
		}

	}
	tx.Abort()

}

func BenchmarkFlatBuffersAdd(b *testing.B) {

	var (
		data, raw []byte

		item = &BinItem{}
		bin  = &Bin{}
		key  = []byte{1}
	)
	CreateBin(key)

	for i := 0; i < b.N; i++ {
		tx, err := env.BeginTxn(nil, WriteTx)
		if err != nil {
			panic(err)
		}

		tx.RawRead = true

		raw, err = tx.Get(db, key)
		if err != nil {
			panic(err)
		}

		data, err = tx.PutReserve(db, key, len(raw), 0)
		if err != nil {
			panic(err)
		}

		copy(data, raw)

		n := fb.GetUOffsetT(data)
		bin.Init(data, n)

		search := uint64(i%BinItemCount + ProductIDOffset)
		for j := 0; j < bin.ItemsLength(); j++ {
			bin.Items(item, j)
			if item.ItemID() == search {
				item.MutateCount(item.Count() + 1)
				break
			}
		}
		tx.Commit()
	}

}

func BenchmarkFlatBuffersAddRemove(b *testing.B) {

	var (
		data []byte

		item = &BinItem{}
		bin  = &Bin{}
		key  = []byte{1}
	)
	CreateBin(key)
	builder := fb.NewBuilder(1024)

	for i := 0; i < b.N; i++ {
		tx, err := env.BeginTxn(nil, WriteTx)
		if err != nil {
			panic(err)
		}

		data, err = tx.Get(db, key)

		if err != nil {
			panic(err)
		}

		n := fb.GetUOffsetT(data)
		bin.Init(data, n)

		search := uint64(i%BinItemCount + ProductIDOffset)
		binCount := bin.ItemsLength()
		found := false

		for j := 0; j < binCount; j++ {
			bin.Items(item, j)
			if item.ItemID() == search {
				count := item.Count()
				if count > 1 {
					item.MutateCount(count - 1)
					found = true
					break
				}
				if count == 1 {
					builder.Reset()
					BinStartItemsVector(builder, binCount-1)
					for k := 0; k < binCount; k++ {
						bin.Items(item, k)
						id := item.ItemID()
						if id != search {
							CreateBinItem(builder, id, item.ShipmentID(), item.Count(), item.Type())
						}
					}
					bins := builder.EndVector(binCount - 1)
					code := builder.CreateString(string(bin.Code()))

					BinStart(builder)
					BinAddItems(builder, bins)
					BinAddType(builder, bin.Type())
					BinAddFlags(builder, bin.Flags())
					BinAddCode(builder, code)
					BinAddSubtype(builder, bin.Subtype())
					BinAddSaleID(builder, bin.SaleID())
					nb := BinEnd(builder)
					builder.Finish(nb)
					found = true
					data = builder.FinishedBytes()
					break
				}

			}
		}
		if !found {

			builder.Reset()
			BinStartItemsVector(builder, binCount+1)
			for k := 0; k < binCount; k++ {
				bin.Items(item, k)
				CreateBinItem(builder, item.ItemID(), item.ShipmentID(), item.Count(), item.Type())
			}
			CreateBinItem(builder, search, uint64(i), RestockCount, ItemTypeProduct)
			bins := builder.EndVector(binCount + 1)
			code := builder.CreateString(string(bin.Code()))

			BinStart(builder)
			BinAddItems(builder, bins)
			BinAddType(builder, bin.Type())
			BinAddFlags(builder, bin.Flags())
			BinAddCode(builder, code)
			BinAddSubtype(builder, bin.Subtype())
			BinAddSaleID(builder, bin.SaleID())
			nb := BinEnd(builder)
			builder.Finish(nb)
			data = builder.FinishedBytes()
		}

		tx.Put(db, key, data, 0)

		tx.Commit()
	}
}
